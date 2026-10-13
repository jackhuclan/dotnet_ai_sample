
"use strict";

/* ============================================================
 *  1. 配置区（只需要改这一段）
 * ============================================================ */
const CONFIG = {
  // 后端接口地址
  endpoint: "http://localhost:5114/chat",

  // 下拉框里的模型。要增删模型，改这个数组即可。
  models: [
    { id: "deepseek-chat",     label: "DeepSeek Chat" },
    { id: "deepseek-reasoner", label: "DeepSeek Reasoner（含思维链）" }
  ],
  defaultModel: "deepseek-chat",

  // 如果后端需要鉴权，在这里加请求头，例如：
  // headers: { "Authorization": "Bearer sk-xxxxxxxx" }
  headers: {},

  // 如果后端需要额外字段，写在这里，会合并进请求体，例如：
  // extraBody: { temperature: 0.7, max_tokens: 2048 }
  extraBody: {},

  // 是否请求流式（SSE）
  stream: true,

  // 全局系统提示词，留空则不发送
  systemPrompt: "",

  // 空状态引导卡片
  examplePrompts: [
    "AOI 检测中「过检率」和「漏检率」怎么权衡？给出可落地的判据思路",
    "用 Python 写一个 PL 光谱多峰高斯拟合并输出峰位与半高宽的完整示例",
    "deepseek-chat 和 deepseek-reasoner 分别适合什么任务？给我一个选型对照表"
  ]
};

/* ============================================================
 *  2. 常量与状态
 * ============================================================ */
const LS_SESSIONS = "aoi_chat.sessions.v1";
const LS_CURRENT  = "aoi_chat.current.v1";
const LS_THEME    = "aoi_chat.theme.v1";
const LS_MODEL    = "aoi_chat.model.v1";
const DEFAULT_TITLE = "新对话";

const state = {
  sessions: [],
  currentId: null,
  streaming: false,
  abort: null
};

let live = null;          // 正在流式的 AI 消息引用
let stick = true;         // 是否自动贴底
let lastPaint = 0;
let paintTimer = null;

/* ============================================================
 *  3. DOM 引用
 * ============================================================ */
const $ = (id) => document.getElementById(id);
const messagesEl   = $("messages");
const scrollEl     = $("scroll");
const sessionListEl= $("sessionList");
const modelSelect  = $("modelSelect");
const inputEl      = $("input");
const btnSend      = $("btnSend");
const btnNew       = $("btnNew");
const btnTheme     = $("btnTheme");
const btnToggle    = $("btnToggleSidebar");
const overlayEl    = $("overlay");
const footNoteEl   = $("footNote");

/* ============================================================
 *  4. 工具函数
 * ============================================================ */
const isMobile = () => window.matchMedia("(max-width:820px)").matches;

function escapeHtml(s){
  return String(s).replace(/[&<>"']/g, (c) => (
    { "&":"&amp;", "<":"&lt;", ">":"&gt;", '"':"&quot;", "'":"&#39;" }[c]
  ));
}

function uid(){
  return Date.now().toString(36) + Math.random().toString(36).slice(2, 8);
}

function truncate(s, n){
  s = String(s).replace(/\s+/g, " ").trim();
  return s.length > n ? s.slice(0, n) + "…" : s;
}

async function copyText(text){
  try{
    if (navigator.clipboard && window.isSecureContext){
      await navigator.clipboard.writeText(text);
      return;
    }
  }catch(_){ /* 回落 */ }
  const ta = document.createElement("textarea");
  ta.value = text;
  ta.readOnly = true;
  ta.style.position = "fixed";
  ta.style.top = "-1000px";
  ta.style.opacity = "0";
  document.body.appendChild(ta);
  ta.select();
  try{ document.execCommand("copy"); }catch(_){}
  document.body.removeChild(ta);
}

/* ============================================================
 *  5. 轻量语法着色
 *     思路：先按「注释 / 字符串 / 数字 / 关键字」整体匹配原始代码，
 *     再逐段 escape 后包 span。因为是在原始文本上匹配、
 *     在回调里才转义，所以插入的标签不会被二次匹配。
 * ============================================================ */
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

/* ============================================================
 *  7. 消息渲染
 * ============================================================ */
let msgIndexSeq = 0;

function buildUserEl(content){
  const el = document.createElement("div");
  el.className = "msg user";
  const inner = document.createElement("div");
  inner.className = "msg-inner";
  const bubble = document.createElement("div");
  bubble.className = "user-bubble";
  bubble.textContent = content;      // 用户消息按纯文本渲染，不做 Markdown
  inner.appendChild(bubble);
  el.appendChild(inner);
  return el;
}

function createAssistantEl(){
  const el = document.createElement("div");
  el.className = "msg assistant";
  const inner = document.createElement("div");
  inner.className = "msg-inner";

  const details = document.createElement("details");
  details.className = "reasoning";
  details.hidden = true;
  const summary = document.createElement("summary");
  const label = document.createElement("span");
  label.className = "reasoning-label";
  summary.appendChild(label);
  const rbody = document.createElement("div");
  rbody.className = "reasoning-body";
  details.appendChild(summary);
  details.appendChild(rbody);

  const content = document.createElement("div");
  content.className = "bubble markdown";

  const actions = document.createElement("div");
  actions.className = "msg-actions";
  const copyBtn = document.createElement("button");
  copyBtn.type = "button";
  copyBtn.dataset.copyMsg = "1";
  copyBtn.textContent = "复制";
  actions.appendChild(copyBtn);

  inner.appendChild(details);
  inner.appendChild(content);
  inner.appendChild(actions);
  el.appendChild(inner);

  return { el, contentEl: content, detailsEl: details, rbodyEl: rbody, labelEl: label };
}

function appendCursor(container){
  const last = container.lastElementChild;
  if (!last) return;
  if (last.classList.contains("code-block")){
    if (last.dataset.streaming === "1") return;   // 代码块自己已带光标
    const code = last.querySelector("pre code");
    if (code){
      const c = document.createElement("span");
      c.className = "cursor";
      code.appendChild(c);
    }
    return;
  }
  const c = document.createElement("span");
  c.className = "cursor";
  last.appendChild(c);
}

function reasoningLabel(msg, streaming){
  if (!msg.reasoning) return "";
  if (streaming && !msg.reasoningEnd){
    const sec = msg.reasoningStart ? Math.max(1, Math.round((Date.now() - msg.reasoningStart) / 1000)) : 0;
    return sec ? "正在深度思考 " + sec + " 秒…" : "正在深度思考…";
  }
  const sec = Math.max(1, Math.round((msg.reasoningMs || 0) / 1000));
  return "已深度思考 " + sec + " 秒";
}

function paintAssistant(refs, msg, streaming){
  // 思考链
  if (msg.reasoning){
    refs.detailsEl.hidden = false;
    refs.labelEl.textContent = reasoningLabel(msg, streaming);
    if (refs.rbodyEl.textContent !== msg.reasoning) refs.rbodyEl.textContent = msg.reasoning;
  }else{
    refs.detailsEl.hidden = true;
  }

  // 错误态
  if (msg.error){
    refs.el.classList.add("has-error");
    refs.contentEl.classList.add("error-bubble");
    refs.contentEl.innerHTML = renderError(msg.error);
    return;
  }
  refs.el.classList.remove("has-error");
  refs.contentEl.classList.remove("error-bubble");

  // 正文
  if (msg.content){
    refs.contentEl.innerHTML = renderMarkdown(msg.content);
    if (streaming) appendCursor(refs.contentEl);
  }else if (streaming){
    refs.contentEl.innerHTML = '<span class="dots"><i></i><i></i><i></i></span>';
  }else{
    refs.contentEl.innerHTML = '<span class="muted">（后端未返回内容）</span>';
  }
}

function renderError(err){
  let html = '<div class="err-title">' + escapeHtml(err.title || "请求失败") + "</div>";
  if (err.detail) html += '<div class="err-detail">' + escapeHtml(err.detail) + "</div>";
  if (err.hint)   html += '<div class="err-hint">' + escapeHtml(err.hint) + "</div>";
  return html;
}

function markInterrupted(refs){
  if (refs.el.querySelector(".interrupted")) return;
  const note = document.createElement("div");
  note.className = "interrupted";
  note.textContent = "— 连接中断 —";
  refs.contentEl.insertAdjacentElement("afterend", note);
}

/* ============================================================
 *  8. 会话与存储
 * ============================================================ */
function currentSession(){
  return state.sessions.find((s) => s.id === state.currentId) || null;
}

function newSession(model){
  return {
    id: uid(),
    title: DEFAULT_TITLE,
    model: model || CONFIG.defaultModel,
    messages: [],
    createdAt: Date.now(),
    updatedAt: Date.now()
  };
}

function cleanSession(s){
  return {
    id: s.id,
    title: s.title,
    model: s.model,
    createdAt: s.createdAt,
    updatedAt: s.updatedAt,
    messages: s.messages
      .filter((m) => !m.error && (m.content || m.reasoning))
      .map((m) => ({
        role: m.role,
        content: m.content || "",
        reasoning: m.reasoning || "",
        reasoningMs: m.reasoningMs || 0,
        interrupted: !!m.interrupted
      }))
  };
}

function saveState(){
  try{
    localStorage.setItem(LS_SESSIONS, JSON.stringify(state.sessions.map(cleanSession)));
    localStorage.setItem(LS_CURRENT, state.currentId || "");
  }catch(e){
    // 多半是 localStorage 配额满了
    footNoteEl.textContent = "本地存储已满，历史会话可能未保存。";
  }
}

function loadState(){
  let sessions = [];
  try{
    const raw = localStorage.getItem(LS_SESSIONS);
    if (raw) sessions = JSON.parse(raw) || [];
  }catch(_){ sessions = []; }

  if (!Array.isArray(sessions)) sessions = [];

  sessions = sessions.filter((s) => s && typeof s === "object").map((s) => ({
    id: s.id || uid(),
    title: s.title || DEFAULT_TITLE,
    model: s.model || CONFIG.defaultModel,
    createdAt: s.createdAt || Date.now(),
    updatedAt: s.updatedAt || Date.now(),
    messages: Array.isArray(s.messages) ? s.messages.map((m) => ({
      role: m.role === "assistant" ? "assistant" : "user",
      content: m.content || "",
      reasoning: m.reasoning || "",
      reasoningMs: m.reasoningMs || 0,
      interrupted: !!m.interrupted
    })) : []
  }));

  state.sessions = sessions;

  const savedId = localStorage.getItem(LS_CURRENT);
  if (savedId && sessions.some((s) => s.id === savedId)) state.currentId = savedId;
  else if (sessions.length) state.currentId = sessions[0].id;

  if (!state.sessions.length){
    const s = newSession(localStorage.getItem(LS_MODEL) || CONFIG.defaultModel);
    state.sessions.push(s);
    state.currentId = s.id;
  }
}

/* ============================================================
 *  9. 侧边栏渲染
 * ============================================================ */
function renderSidebar(){
  sessionListEl.innerHTML = "";
  const sorted = state.sessions.slice().sort((a, b) => b.updatedAt - a.updatedAt);

  const label = document.createElement("div");
  label.className = "group-label";
  label.textContent = "历史对话 · " + state.sessions.length;
  sessionListEl.appendChild(label);

  sorted.forEach((s) => {
    const row = document.createElement("div");
    row.className = "sess" + (s.id === state.currentId ? " active" : "");
    row.dataset.id = s.id;
    row.title = s.title;

    const t = document.createElement("span");
    t.className = "sess-title";
    t.textContent = s.title;

    const del = document.createElement("button");
    del.className = "sess-del";
    del.type = "button";
    del.title = "删除该对话";
    del.textContent = "×";
    del.dataset.del = "1";

    row.appendChild(t);
    row.appendChild(del);
    sessionListEl.appendChild(row);
  });
}

/* ============================================================
 * 10. 对话渲染
 * ============================================================ */
function buildEmptyState(){
  const wrap = document.createElement("div");
  wrap.className = "empty";
  wrap.innerHTML =
    '<div class="empty-logo">D</div>' +
    "<h2>今天想聊点什么？</h2>" +
    '<div class="prompt-cards"></div>';
  const cards = wrap.querySelector(".prompt-cards");
  CONFIG.examplePrompts.forEach((p) => {
    const b = document.createElement("button");
    b.className = "prompt-card";
    b.type = "button";
    b.dataset.prompt = p;
    b.textContent = p;
    cards.appendChild(b);
  });
  return wrap;
}

function renderConversation(){
  const s = currentSession();
  messagesEl.innerHTML = "";
  live = null;
  if (!s || !s.messages.length){
    messagesEl.appendChild(buildEmptyState());
    updateFootNote();
    return;
  }

  s.messages.forEach((m) => {
    if (m.role === "user"){
      messagesEl.appendChild(buildUserEl(m.content));
    }else{
      const refs = createAssistantEl();
      paintAssistant(refs, m, false);
      if (m.interrupted) markInterrupted(refs);
      messagesEl.appendChild(refs.el);
    }
  });
  scrollToBottom(true);
  updateFootNote();
}

function updateFootNote(){
  const s = currentSession();
  if (!s) { footNoteEl.textContent = ""; return; }
  footNoteEl.textContent = "接口：" + CONFIG.endpoint;
  document.title = (s.title === DEFAULT_TITLE ? "DeepSeek Chat" : s.title + " · DeepSeek Chat");
}

function scrollToBottom(force){
  if (force || stick) scrollEl.scrollTop = scrollEl.scrollHeight;
}

/* ============================================================
 * 11. 流式渲染节流（约 80ms 一帧，避免长文卡顿）
 * ============================================================ */
function schedulePaint(){
  if (paintTimer) return;
  const now = performance.now();
  const wait = Math.max(0, 80 - (now - lastPaint));
  paintTimer = setTimeout(() => {
    paintTimer = null;
    lastPaint = performance.now();
    if (live) paintAssistant(live, liveMsg, true);
    scrollToBottom(false);
  }, wait);
}

let liveMsg = null;

/* ============================================================
 * 12. 发送与流式接收
 * ============================================================ */
function setSendButton(streaming){
  if (streaming){
    btnSend.classList.add("stop");
    btnSend.title = "停止生成";
    btnSend.disabled = false;
    btnSend.innerHTML = '<svg viewBox="0 0 24 24" fill="currentColor"><rect x="7" y="7" width="10" height="10" rx="1.6"/></svg>';
    inputEl.placeholder = "正在生成…";
  }else{
    btnSend.classList.remove("stop");
    btnSend.title = "发送";
    btnSend.disabled = inputEl.value.trim().length === 0;
    btnSend.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M12 19V5M5 12l7-7 7 7"/></svg>';
    inputEl.placeholder = "给 DeepSeek 发送消息…";
  }
}

function autoGrow(){
  inputEl.style.height = "auto";
  inputEl.style.height = Math.min(inputEl.scrollHeight, 220) + "px";
  if (!state.streaming) btnSend.disabled = inputEl.value.trim().length === 0;
}

/* ---- 从任意形状的对象里抽取文本 ---- */
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

/* ---- SSE 行解析器：把任意切分的字节流还原成一条条 data: 负载 ----
 * 真实网络里一行 JSON 常被 TCP 从中间切开，所以必须按 \n 边界缓冲。
 * 抽成纯函数是为了能脱离浏览器单独测试。
 */
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

function buildPayload(session){
  // 占位的那条 assistant 消息（数组最后一条）不参与请求
  const msgs = session.messages.slice(0, session.messages.length - 1)
    .filter((m) => !m.error && (m.role === "user" || m.role === "assistant"))
    .map((m) => ({ role: m.role, content: m.content }));

  if (CONFIG.systemPrompt) msgs.unshift({ role: "system", content: CONFIG.systemPrompt });

  const body = { model: session.model, messages: msgs, stream: !!CONFIG.stream };
  return Object.assign(body, CONFIG.extraBody || {});
}

function httpHint(status){
  if (status === 404) return "接口路径可能不对。请检查 CONFIG.endpoint，当前为 " + CONFIG.endpoint;
  if (status === 401 || status === 403) return "鉴权失败。如果后端需要 Token，请在 CONFIG.headers 里补上。";
  if (status === 405) return "请求方法不被允许，后端可能只接受 POST。";
  if (status === 422) return "请求体格式不被接受。请确认后端期望的字段名，必要时用 CONFIG.extraBody 调整。";
  if (status >= 500) return "后端内部错误，请查看后端日志。";
  return "";
}

const CORS_HINT = "网络层面失败，常见原因：① 后端没启动，确认 " + CONFIG.endpoint +
  " 可访问；② 跨域被浏览器拦截，后端需返回 Access-Control-Allow-Origin 并正确处理 OPTIONS 预检；" +
  "③ 页面以 file:// 打开时跨域限制更严，建议让后端直接托管本页面。";

async function send(){
  if (state.streaming) return;
  const text = inputEl.value.trim();
  if (!text) return;

  const session = currentSession();
  if (!session) return;

  // 1) 落用户消息
  session.messages.push({ role: "user", content: text });
  if (session.title === DEFAULT_TITLE) session.title = truncate(text, 20);
  session.updatedAt = Date.now();

  inputEl.value = "";
  autoGrow();

  if (messagesEl.querySelector(".empty")) messagesEl.innerHTML = "";
  messagesEl.appendChild(buildUserEl(text));

  // 2) 落 AI 占位消息
  const msg = {
    role: "assistant",
    content: "",
    reasoning: "",
    reasoningStart: 0,
    reasoningEnd: 0,
    reasoningMs: 0,
    error: null,
    interrupted: false
  };
  session.messages.push(msg);

  const refs = createAssistantEl();
  messagesEl.appendChild(refs.el);
  live = refs;
  liveMsg = msg;

  renderSidebar();
  stick = true;
  scrollToBottom(true);
  paintAssistant(refs, msg, true);

  await runStream(session, msg, refs);

  // 3) 收尾
  live = null;
  liveMsg = null;
  if (msg.reasoningStart && !msg.reasoningEnd) msg.reasoningEnd = Date.now();
  if (msg.reasoningStart) msg.reasoningMs = (msg.reasoningEnd || Date.now()) - msg.reasoningStart;
  paintAssistant(refs, msg, false);
  if (msg.interrupted) markInterrupted(refs);
  session.updatedAt = Date.now();
  saveState();
  renderSidebar();
  updateFootNote();
  scrollToBottom(false);
}

async function runStream(session, msg, refs){
  state.streaming = true;
  setSendButton(true);

  const ac = new AbortController();
  state.abort = ac;

  let sawSSE = false;
  let rawAll = "";

  const applyChunk = (piece) => {
    if (piece.reasoning){
      if (!msg.reasoningStart) msg.reasoningStart = Date.now();
      msg.reasoning += piece.reasoning;
    }
    if (piece.content){
      if (msg.reasoningStart && !msg.reasoningEnd) msg.reasoningEnd = Date.now();
      msg.content += piece.content;
    }
    if (piece.content || piece.reasoning) schedulePaint();
  };

  const consumeDataPayload = (payload) => {
    const p = payload.trim();
    if (!p) return;
    if (p === "[DONE]" || p === "DONE") return;
    try{
      const obj = JSON.parse(p);
      applyChunk(extractFromObject(obj, 0));
    }catch(_){
      // 不是 JSON，就当纯文本增量
      applyChunk({ content: p, reasoning: "" });
    }
  };

  try{
    const res = await fetch(CONFIG.endpoint, {
      method: "POST",
      headers: Object.assign({ "Content-Type": "application/json" }, CONFIG.headers || {}),
      body: JSON.stringify(buildPayload(session)),
      signal: ac.signal
    });

    if (!res.ok){
      let detail = "";
      try{ detail = (await res.text()).slice(0, 800); }catch(_){}
      msg.error = {
        title: "请求失败：HTTP " + res.status + " " + (res.statusText || ""),
        detail: detail,
        hint: httpHint(res.status)
      };
      return;
    }

    // ---- 情况 A：非流式，一次性返回 ----
    const ctype = (res.headers.get("content-type") || "").toLowerCase();
    if (!res.body || (ctype.indexOf("event-stream") < 0 && !CONFIG.stream)){
      const text = await res.text();
      let handled = false;
      if (text.indexOf("data:") >= 0){
        // 名义上不是 event-stream，但内容是 SSE
        text.split("\n").forEach((ln) => {
          const t = ln.trim();
          if (t.indexOf("data:") === 0){ sawSSE = true; consumeDataPayload(t.slice(5)); }
        });
        handled = sawSSE;
      }
      if (!handled){
        try{
          applyChunk(extractFromObject(JSON.parse(text), 0));
        }catch(_){
          applyChunk({ content: text, reasoning: "" });
        }
      }
      if (!msg.content && !msg.reasoning){
        msg.error = { title: "后端返回了空内容", detail: text.slice(0, 800), hint: "请检查后端返回体的字段名。" };
      }
      return;
    }

    // ---- 情况 B：SSE 流式 ----
    const reader = res.body.getReader();
    const decoder = new TextDecoder("utf-8");
    const parser = createSSEParser(consumeDataPayload);

    while (true){
      const r = await reader.read();
      if (r.done) break;
      parser.push(decoder.decode(r.value, { stream: true }));
    }

    const fin = parser.flush();
    sawSSE = fin.sawSSE;
    rawAll = fin.raw;

    // 兜底：不是标准 SSE（没有 data: 前缀），整体解析
    if (!sawSSE && rawAll.trim()){
      const t = rawAll.trim();
      try{
        applyChunk(extractFromObject(JSON.parse(t), 0));
      }catch(_){
        applyChunk({ content: t, reasoning: "" });
      }
    }

    if (!msg.content && !msg.reasoning){
      msg.error = {
        title: "后端没有返回可识别的文本",
        detail: (rawAll || "（响应体为空）").slice(0, 800),
        hint: "请检查后端返回体的字段名是否为 choices[0].delta.content，若是自定义字段请告诉我，我来加适配。"
      };
    }

  }catch(err){
    if (err && err.name === "AbortError"){
      msg.interrupted = true;
    }else{
      msg.error = {
        title: "网络请求失败：" + ((err && err.message) || "未知错误"),
        detail: "",
        hint: CORS_HINT
      };
    }
  }finally{
    state.streaming = false;
    state.abort = null;
    setSendButton(false);
    if (msg.reasoningStart && !msg.reasoningEnd) msg.reasoningEnd = Date.now();
    if (msg.reasoningStart) msg.reasoningMs = (msg.reasoningEnd || Date.now()) - msg.reasoningStart;
    paintAssistant(refs, msg, false);
    if (msg.interrupted) markInterrupted(refs);
    scrollToBottom(false);
  }
}

/* ============================================================
 * 13. 事件绑定
 * ============================================================ */
btnSend.addEventListener("click", () => {
  if (state.streaming){
    if (state.abort) state.abort.abort();
  }else{
    send();
  }
});

inputEl.addEventListener("input", autoGrow);

inputEl.addEventListener("keydown", (e) => {
  if (e.key === "Enter" && !e.shiftKey && !e.isComposing && e.keyCode !== 229){
    e.preventDefault();
    if (!state.streaming) send();
  }
});

scrollEl.addEventListener("scroll", () => {
  const d = scrollEl.scrollHeight - scrollEl.scrollTop - scrollEl.clientHeight;
  stick = d < 60;
});

// 代码块复制 / 消息复制 / 示例卡片
messagesEl.addEventListener("click", (e) => {
  const t = e.target;

  const codeBtn = t.closest ? t.closest("[data-copy]") : null;
  if (codeBtn){
    const block = codeBtn.closest(".code-block");
    const code = block ? block.querySelector("pre code") : null;
    copyText(code ? code.textContent : "").then(() => {
      codeBtn.textContent = "已复制";
      setTimeout(() => { codeBtn.textContent = "复制"; }, 1200);
    });
    return;
  }

  const msgBtn = t.closest ? t.closest("[data-copy-msg]") : null;
  if (msgBtn){
    const msgEl = msgBtn.closest(".msg");
    const inner = msgEl ? msgEl.querySelector(".bubble") : null;
    copyText(inner ? inner.innerText : "").then(() => {
      msgBtn.textContent = "已复制";
      setTimeout(() => { msgBtn.textContent = "复制"; }, 1200);
    });
    return;
  }

  const card = t.closest ? t.closest(".prompt-card") : null;
  if (card){
    inputEl.value = card.dataset.prompt || "";
    autoGrow();
    inputEl.focus();
    return;
  }
});

// 会话列表
sessionListEl.addEventListener("click", (e) => {
  const del = e.target.closest ? e.target.closest("[data-del]") : null;
  const row = e.target.closest ? e.target.closest(".sess") : null;
  if (!row) return;
  const id = row.dataset.id;

  if (del){
    const s = state.sessions.find((x) => x.id === id);
    if (!s) return;
    if (!confirm("确定删除对话「" + s.title + "」？")) return;
    state.sessions = state.sessions.filter((x) => x.id !== id);
    if (state.currentId === id){
      if (!state.sessions.length) state.sessions.push(newSession(s.model));
      state.currentId = state.sessions[0].id;
    }
    saveState();
    renderSidebar();
    renderConversation();
    syncModelSelect();
    return;
  }

  if (id !== state.currentId){
    if (state.streaming && state.abort) state.abort.abort();
    state.currentId = id;
    const s = currentSession();
    if (s) modelSelect.value = s.model;
    saveState();
    renderSidebar();
    renderConversation();
    stick = true;
    scrollToBottom(true);
  }
  if (isMobile()) document.body.classList.add("collapsed");
});

btnNew.addEventListener("click", () => {
  if (state.streaming && state.abort) state.abort.abort();
  const model = modelSelect.value || localStorage.getItem(LS_MODEL) || CONFIG.defaultModel;
  const s = newSession(model);
  state.sessions.push(s);
  state.currentId = s.id;
  saveState();
  renderSidebar();
  renderConversation();
  syncModelSelect();
  inputEl.focus();
  if (isMobile()) document.body.classList.add("collapsed");
});

modelSelect.addEventListener("change", () => {
  const s = currentSession();
  if (s){ s.model = modelSelect.value; saveState(); }
  try{ localStorage.setItem(LS_MODEL, modelSelect.value); }catch(_){}
});

// 侧边栏开合
btnToggle.addEventListener("click", () => {
  document.body.classList.toggle("collapsed");
});
overlayEl.addEventListener("click", () => {
  document.body.classList.add("collapsed");
});

// 主题
function applyTheme(theme){
  document.documentElement.dataset.theme = theme;
  btnTheme.textContent = theme === "dark" ? "☀️" : "🌙";
  btnTheme.title = theme === "dark" ? "切换到浅色" : "切换到深色";
}
btnTheme.addEventListener("click", () => {
  const next = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
  applyTheme(next);
  try{ localStorage.setItem(LS_THEME, next); }catch(_){}
});

window.addEventListener("resize", () => { autoGrow(); });

/* ============================================================
 * 14. 初始化
 * ============================================================ */
function syncModelSelect(){
  const s = currentSession();
  if (!s) return;
  if (CONFIG.models.some((m) => m.id === s.model)) modelSelect.value = s.model;
  else modelSelect.value = CONFIG.defaultModel;
}

function initTheme(){
  let theme = "light";
  try{ theme = localStorage.getItem(LS_THEME) || "light"; }catch(_){}
  applyTheme(theme);
}

function initModels(){
  modelSelect.innerHTML = "";
  CONFIG.models.forEach((m) => {
    const o = document.createElement("option");
    o.value = m.id;
    o.textContent = m.label;
    modelSelect.appendChild(o);
  });
}

function init(){
  initTheme();
  initModels();
  loadState();
  syncModelSelect();
  // 初始：桌面展开，移动端收起
  document.body.classList.toggle("collapsed", isMobile());
  renderSidebar();
  renderConversation();
  setSendButton(false);
  autoGrow();
}

init();
