function escapeHtml(s){
  return String(s).replace(/[&<>"']/g, (c) => (
    { "&":"&amp;", "<":"&lt;", ">":"&gt;", '"':"&quot;", "'":"&#39;" }[c]
  ));
}
const KEYWORD_SRC = [
  "abstract","as","assert","async","await","bool","break","byte","case","catch","chan",
  "char","class","const","continue","def","default","defer","del","delete","do","double",
  "elif","else","enum","except","export","extends","extern","false","final","finally",
  "float","fn","for","from","func","function","global","go","if","impl","implements",
  "import","in","instanceof","int","interface","is","lambda","let","long","match","module",
  "mut","namespace","new","nil","none","null","object","package","pass","private","protected",
  "pub","public","raise","range","readonly","return","self","short","signed","static","str",
  "struct","super","switch","this","throw","throws","trait","True","False","None","true",
  "false","try","type","typeof","undefined","union","unsigned","use","var","void","volatile",
  "when","where","while","with","yield"
].join("|");

const HASH_COMMENT_LANGS = ["py","python","rb","ruby","sh","bash","zsh","shell","yaml","yml",
  "toml","ini","conf","dockerfile","makefile","r","perl","pl","ps1","powershell","cmake"];
const DASH_COMMENT_LANGS = ["sql","lua","haskell","ada","elm"];

function highlightCode(code, lang){
  const L = String(lang || "").toLowerCase();
  const hash = HASH_COMMENT_LANGS.indexOf(L) >= 0;
  const dash = DASH_COMMENT_LANGS.indexOf(L) >= 0;

  // 分组是动态拼的，所以捕获组下标必须同步记录，不能写死
  const parts = [];
  const kinds = [];

  parts.push("(\\/\\/[^\\n]*|\\/\\*[\\s\\S]*?\\*\\/)");                    kinds.push("c");
  if (hash){ parts.push("(#[^\\n]*)");                                   kinds.push("c"); }
  if (dash){ parts.push("(--[^\\n]*)");                                  kinds.push("c"); }
  parts.push("(\"(?:[^\"\\\\\\n]|\\\\.)*\"|'(?:[^'\\\\\\n]|\\\\.)*'|`(?:[^`\\\\]|\\\\.)*`)"); kinds.push("s");
  parts.push("(\\b\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?\\b)");                 kinds.push("n");
  parts.push("(\\b(?:" + KEYWORD_SRC + ")\\b)");                         kinds.push("k");

  let re;
  try{ re = new RegExp(parts.join("|"), "g"); }catch(_){ return escapeHtml(code); }

  let out = "";
  let last = 0;
  let m;
  while ((m = re.exec(code)) !== null){
    if (m[0] === ""){ re.lastIndex++; continue; }
    out += escapeHtml(code.slice(last, m.index));
    let cls = "";
    for (let g = 0; g < kinds.length; g++){
      if (m[g + 1] != null){ cls = "tok-" + kinds[g]; break; }
    }
    out += cls ? '<span class="' + cls + '">' + escapeHtml(m[0]) + "</span>" : escapeHtml(m[0]);
    last = m.index + m[0].length;
  }
  out += escapeHtml(code.slice(last));
  return out;
}

/* ============================================================
 *  6. Markdown 渲染（零依赖，约 200 行）
 * ============================================================ */
const CODE_TOKEN = "\uE000";

function inline(text){
  const codes = [];
  // 先抽出行内代码，避免内部内容被后续规则破坏
  let s = text.replace(/`([^`]+)`/g, (_, c) => {
    codes.push(c);
    return CODE_TOKEN + (codes.length - 1) + CODE_TOKEN;
  });
  // 链接
  s = s.replace(/\[([^\]]+)\]\(([^)\s]+)\)/g,
    (_, t, u) => '<a href="' + u + '" target="_blank" rel="noopener noreferrer">' + t + "</a>");
  // 粗体
  s = s.replace(/(\*\*|__)(?=\S)([\s\S]*?\S)\1/g, "<strong>$2</strong>");
  // 斜体
  s = s.replace(/(^|[^*])\*(?=\S)([^*]*?\S)\*(?!\*)/g, "$1<em>$2</em>");
  s = s.replace(/(^|[^_\w])_(?=\S)([^_]*?\S)_(?![\w])/g, "$1<em>$2</em>");
  // 删除线
  s = s.replace(/~~(?=\S)([\s\S]*?\S)~~/g, "<del>$1</del>");
  // 还原行内代码
  const re = new RegExp(CODE_TOKEN + "(\\d+)" + CODE_TOKEN, "g");
  s = s.replace(re, (_, i) => '<code class="inline-code">' + codes[+i] + "</code>");
  return s;
}

function renderCodeBlock(code, lang, stillOpen){
  const label = lang || "text";
  const body = lang ? highlightCode(code, lang) : escapeHtml(code);
  return '<div class="code-block"' + (stillOpen ? ' data-streaming="1"' : "") + ">"
       +   '<div class="code-head">'
       +     '<span class="code-lang">' + escapeHtml(label) + "</span>"
       +     '<button class="code-copy" type="button" data-copy="1">复制</button>'
       +   "</div>"
       +   '<pre class="code-body"><code>' + body + "</code></pre>"
       + "</div>";
}

function splitRow(line){
  return line.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map((c) => c.trim());
}

function renderMarkdown(src){
  if (!src) return "";
  const lines = String(src).replace(/\r\n?/g, "\n").split("\n");
  const out = [];
  let i = 0;

  const isBlockStart = (l) =>
    /^\s*(```|~~~)/.test(l) ||
    /^#{1,6}\s/.test(l) ||
    /^\s*>\s?/.test(l) ||
    /^\s*([-*+]|\d+[.)])\s+/.test(l) ||
    /^\s*([-*_])\s*(\1\s*){2,}$/.test(l);

  while (i < lines.length){
    const line = lines[i];

    // ---- 围栏代码块（含未闭合的流式半截块）----
    const fence = line.match(/^\s*(```|~~~)\s*([\w+#.\-]*)\s*$/);
    if (fence){
      const marker = fence[1];
      const lang = (fence[2] || "").toLowerCase();
      const buf = [];
      i++;
      let closed = false;
      const closeRe = new RegExp("^\\s*" + marker + "\\s*$");
      while (i < lines.length){
        if (closeRe.test(lines[i])){ closed = true; i++; break; }
        buf.push(lines[i]);
        i++;
      }
      out.push(renderCodeBlock(buf.join("\n"), lang, !closed));
      continue;
    }

    // ---- 分割线 ----
    if (/^\s*([-*_])\s*(\1\s*){2,}$/.test(line)){ out.push("<hr>"); i++; continue; }

    // ---- 标题 ----
    const h = line.match(/^(#{1,6})\s+(.*)$/);
    if (h){
      const lvl = h[1].length;
      out.push("<h" + lvl + ">" + inline(escapeHtml(h[2].trim())) + "</h" + lvl + ">");
      i++;
      continue;
    }

    // ---- 引用 ----
    if (/^\s*>\s?/.test(line)){
      const buf = [];
      while (i < lines.length && /^\s*>\s?/.test(lines[i])){
        buf.push(lines[i].replace(/^\s*>\s?/, ""));
        i++;
      }
      out.push("<blockquote>" + renderMarkdown(buf.join("\n")) + "</blockquote>");
      continue;
    }

    // ---- 表格 ----
    if (line.indexOf("|") >= 0 && i + 1 < lines.length){
      const head = splitRow(line);
      const sepCells = splitRow(lines[i + 1]);
      const isTable = sepCells.length === head.length &&
                      sepCells.every((c) => /^:?-{1,}:?$/.test(c));
      if (isTable){
        const aligns = sepCells.map((c) => {
          const l = c.charAt(0) === ":";
          const r = c.charAt(c.length - 1) === ":";
          return l && r ? "center" : r ? "right" : l ? "left" : "";
        });
        let html = "<table><thead><tr>";
        head.forEach((c, k) => {
          html += '<th' + (aligns[k] ? ' style="text-align:' + aligns[k] + '"' : "") + ">"
                + inline(escapeHtml(c)) + "</th>";
        });
        html += "</tr></thead><tbody>";
        i += 2;
        while (i < lines.length && lines[i].trim() && lines[i].indexOf("|") >= 0 &&
               !/^\s*(```|~~~)/.test(lines[i])){
          const cells = splitRow(lines[i]);
          html += "<tr>";
          for (let k = 0; k < head.length; k++){
            html += "<td" + (aligns[k] ? ' style="text-align:' + aligns[k] + '"' : "") + ">"
                  + inline(escapeHtml(cells[k] == null ? "" : cells[k])) + "</td>";
          }
          html += "</tr>";
          i++;
        }
        html += "</tbody></table>";
        out.push(html);
        continue;
      }
    }

    // ---- 列表（支持缩进嵌套）----
    if (/^\s*([-*+]|\d+[.)])\s+/.test(line)){
      const res = parseList(lines, i);
      out.push(res.html);
      i = res.next;
      continue;
    }

    // ---- 空行 ----
    if (/^\s*$/.test(line)){ i++; continue; }

    // ---- 段落 ----
    const buf = [];
    while (i < lines.length && lines[i].trim() && !isBlockStart(lines[i])){ buf.push(lines[i]); i++; }
    out.push("<p>" + inline(escapeHtml(buf.join("\n"))).replace(/\n/g, "<br>") + "</p>");
  }
  return out.join("\n");
}

function parseList(lines, start){
  const itemRe = /^(\s*)([-*+]|\d+[.)])\s+(.*)$/;
  const first = lines[start].match(itemRe);
  const baseIndent = first[1].length;
  const ordered = /\d/.test(first[2]);
  const items = [];
  let i = start;

  while (i < lines.length){
    const m = lines[i].match(itemRe);
    if (!m || m[1].length < baseIndent) break;

    if (m[1].length > baseIndent){
      // 更深的缩进 → 归为上一项的嵌套列表
      const nested = parseList(lines, i);
      if (items.length) items[items.length - 1].children.push(nested.html);
      i = nested.next;
      continue;
    }
    if (/\d/.test(m[2]) !== ordered) break;   // 同级但换了列表类型

    items.push({ text: m[3], children: [] });
    i++;
    // 把列表项的续行并进来
    while (i < lines.length && /^\s+\S/.test(lines[i]) && !itemRe.test(lines[i])){
      items[items.length - 1].text += "\n" + lines[i].trim();
      i++;
    }
  }

  const tag = ordered ? "ol" : "ul";
  const html = "<" + tag + ">" + items.map((it) =>
    "<li>" + inline(escapeHtml(it.text)).replace(/\n/g, "<br>") + it.children.join("") + "</li>"
  ).join("") + "</" + tag + ">";
  return { html, next: i };
}


function extractFromObject(obj, depth){
  depth = depth || 0;
  const res = { content: "", reasoning: "" };
  if (obj == null || depth > 4) return res;
  if (typeof obj === "string"){ res.content = obj; return res; }
  if (typeof obj !== "object") return res;

  const ch = Array.isArray(obj.choices) ? obj.choices[0] : null;
  if (ch){
    const d = ch.delta || ch.message || {};
    if (typeof d.content === "string") res.content += d.content;
    if (typeof d.reasoning_content === "string") res.reasoning += d.reasoning_content;
    else if (typeof d.reasoning === "string") res.reasoning += d.reasoning;
    if (!res.content && typeof ch.text === "string") res.content += ch.text;
  }
  if (!res.content){
    const keys = ["content","text","reply","answer","response","message","output","result","delta"];
    for (let i = 0; i < keys.length; i++){
      const v = obj[keys[i]];
      if (typeof v === "string" && v.length){ res.content += v; break; }
    }
  }
  if (!res.reasoning){
    if (typeof obj.reasoning_content === "string") res.reasoning = obj.reasoning_content;
    else if (typeof obj.reasoning === "string") res.reasoning = obj.reasoning;
  }
  // 常见包装层：{ data: {...} } / { data: { content: "..." } }
  if (!res.content && !res.reasoning && obj.data != null){
    const inner = extractFromObject(obj.data, depth + 1);
    res.content += inner.content;
    res.reasoning += inner.reasoning;
  }
  return res;
}
function createSSEParser(onPayload){
  let buffer = "";
  let sawSSE = false;
  let raw = "";

  function feedLine(line){
    if (line.indexOf("data:") === 0){
      sawSSE = true;
      onPayload(line.slice(5).trim());
      return true;
    }
    return false;   // event: / id: / :心跳 / 空行，一律忽略
  }

  return {
    push(text){
      if (!sawSSE) raw += text;
      buffer += text;
      let idx;
      while ((idx = buffer.indexOf("\n")) >= 0){
        const line = buffer.slice(0, idx).replace(/\r$/, "");
        buffer = buffer.slice(idx + 1);
        feedLine(line);
      }
    },
    flush(){
      // 注意：残留已经在 push 里累加进 raw 了，这里不能再加一次
      const rest = buffer.trim();
      buffer = "";
      if (rest) feedLine(rest);
      return { sawSSE: sawSSE, raw: raw };
    }
  };
}
module.exports={renderMarkdown,extractFromObject,highlightCode,escapeHtml,createSSEParser};