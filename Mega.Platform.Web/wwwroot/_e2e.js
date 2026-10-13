/**
 * 端到端测试：用 jsdom 真的加载 index.html，跑完整的「输入 → 发送 → 流式接收 → 渲染」链路。
 * 运行：NODE_PATH=<e2e>/node_modules node _e2e.js
 */
const fs = require("fs");
const path = require("path");
const { JSDOM, VirtualConsole } = require("jsdom");

const HTML_PATH = path.join(__dirname, "index.html");
const html = fs.readFileSync(HTML_PATH, "utf8");

let pass = 0, fail = 0;
const failures = [];
function ok(name, cond, extra) {
  if (cond) { pass++; console.log("  PASS  " + name); }
  else { fail++; failures.push(name); console.log("  FAIL  " + name + (extra ? "\n        -> " + extra : "")); }
}
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

/* ---------- 造假响应 ---------- */
const enc = new TextEncoder();

function res(chunks, ctype, status, statusText) {
  const stream = new ReadableStream({
    start(c) { chunks.forEach((ch) => c.enqueue(ch)); c.close(); }
  });
  return new Response(stream, {
    status: status || 200,
    statusText: statusText || "OK",
    headers: { "Content-Type": ctype || "text/event-stream; charset=utf-8" }
  });
}

// 永不断流的响应，用来测「停止生成」
function resAbortable(chunks, signal) {
  let ctrl;
  const stream = new ReadableStream({
    start(c) { ctrl = c; chunks.forEach((ch) => c.enqueue(ch)); }
  });
  if (signal) {
    signal.addEventListener("abort", () => {
      const e = new Error("aborted");
      e.name = "AbortError";
      try { ctrl.error(e); } catch (_) {}
    });
  }
  return new Response(stream, { status: 200, headers: { "Content-Type": "text/event-stream" } });
}

function frame(obj) { return enc.encode("data: " + JSON.stringify(obj) + "\n\n"); }
function delta(content, reasoning) {
  const d = {};
  if (reasoning) d.reasoning_content = reasoning;
  if (content) d.content = content;
  return { choices: [{ index: 0, delta: d }] };
}

/* ---------- 起一个干净的页面 ---------- */
function boot(impl) {
  const errors = [];
  const vc = new VirtualConsole();
  vc.on("jsdomError", (e) => errors.push(String(e && e.message || e)));
  vc.on("error", (...a) => errors.push(a.join(" ")));

  const calls = [];

  const dom = new JSDOM(html, {
    url: "http://localhost:5199/",
    runScripts: "dangerously",
    pretendToBeVisual: true,
    virtualConsole: vc,
    beforeParse(window) {
      window.TextDecoder = TextDecoder;
      window.TextEncoder = TextEncoder;
      window.ReadableStream = ReadableStream;
      window.Response = Response;
      window.Headers = Headers;
      window.matchMedia = () => ({
        matches: false, media: "", addEventListener() {}, removeEventListener() {},
        addListener() {}, removeListener() {}
      });
      window.fetch = (url, init) => {
        let body = null;
        try { body = JSON.parse(init && init.body); } catch (_) {}
        calls.push({ url: String(url), body, init });
        return impl(String(url), init || {}, calls.length - 1);
      };
    }
  });

  const doc = dom.window.document;
  return { dom, doc, window: dom.window, errors, calls };
}

function type(doc, window, text) {
  const input = doc.getElementById("input");
  input.value = text;
  input.dispatchEvent(new window.Event("input", { bubbles: true }));
}

function clickSend(doc, window) {
  doc.getElementById("btnSend").dispatchEvent(new window.MouseEvent("click", { bubbles: true }));
}

/* ============================================================
 *  场景 1：reasoner 流式（思维链 + 正文 + 代码块）
 * ============================================================ */
async function scenarioStream() {
  console.log("\n=== 场景 1：reasoner 流式，含思维链与代码块 ===");

  const frames = [
    // 故意把一行 JSON 从中间切开，模拟 TCP 半包
    enc.encode('data: {"choices":[{"delta":{"reasoning_cont'),
    enc.encode('ent":"先想一下。"}}]}\n\n'),
    frame(delta("", "再看清楚问题。")),
    frame(delta("答案是 **42**。\n\n")),
    frame(delta("```python\ndef f(x):\n    return x + 1\n```\n")),
    enc.encode("data: [DONE]\n\n")
  ];

  const { doc, window, errors, calls } = boot(() => Promise.resolve(res(frames)));

  const sel = doc.getElementById("modelSelect");
  ok("模型下拉框已填充", sel.options.length === 2, "options=" + sel.options.length);
  ok("默认选中 deepseek-chat", sel.value === "deepseek-chat", sel.value);

  sel.value = "deepseek-reasoner";
  sel.dispatchEvent(new window.Event("change", { bubbles: true }));

  type(doc, window, "过检和漏检怎么权衡？");
  clickSend(doc, window);
  await sleep(500);

  const userBubble = doc.querySelector(".msg.user .user-bubble");
  ok("用户消息渲染为右对齐胶囊", !!userBubble && userBubble.textContent === "过检和漏检怎么权衡？");

  const body = calls[0] && calls[0].body;
  ok("请求体模型正确", !!body && body.model === "deepseek-reasoner", JSON.stringify(body && body.model));
  ok("请求体带 stream 标记", !!body && body.stream === true, JSON.stringify(body && body.stream));
  ok("请求体携带消息", !!body && Array.isArray(body.messages) && body.messages.length === 1, JSON.stringify(body && body.messages));

  const reasoning = doc.querySelector(".msg.assistant .reasoning");
  ok("思维链面板已显示", !!reasoning && reasoning.hidden === false);
  const rtext = doc.querySelector(".reasoning-body").textContent;
  ok("思维链内容拼接正确（跨包半行）",
     rtext.includes("先想一下。") && rtext.includes("再看清楚问题。"), JSON.stringify(rtext));
  ok("思维链默认收起", reasoning && reasoning.open === false);
  ok("思维链标签显示秒数", /已深度思考 \d+ 秒/.test(doc.querySelector(".reasoning-label").textContent),
     doc.querySelector(".reasoning-label").textContent);

  const bubble = doc.querySelector(".msg.assistant .bubble");
  const bh = bubble.innerHTML;
  ok("正文 Markdown 加粗", bh.includes("<strong>42</strong>"), bh.slice(0, 300));
  ok("正文渲染代码块", bh.includes('<div class="code-block"'), bh.slice(0, 300));
  ok("代码块有复制按钮", bh.includes('data-copy="1"'));
  ok("代码块语法着色", bh.includes('tok-k">def</span>'), bh.slice(0, 400));
  ok("流式结束后光标已移除", !bh.includes('class="cursor"'));

  const sess = doc.querySelectorAll(".sess");
  ok("侧边栏出现 1 个会话", sess.length === 1, "n=" + sess.length);
  ok("会话标题取首句前 20 字",
     doc.querySelector(".sess-title").textContent === "过检和漏检怎么权衡？",
     doc.querySelector(".sess-title").textContent);

  const stored = window.localStorage.getItem("aoi_chat.sessions.v1");
  ok("会话已写入 localStorage", !!stored && stored.includes("先想一下。"));
  const parsed = JSON.parse(stored);
  ok("持久化的 assistant 消息含正文与思维链",
     parsed[0].messages[1].content.includes("42") && parsed[0].messages[1].reasoning.includes("再看清楚"),
     JSON.stringify(parsed[0].messages[1]).slice(0, 200));
  ok("持久化结构已剥离临时字段（无 reasoningStart）",
     parsed[0].messages[1].reasoningStart === undefined);

  ok("页面无运行时异常", errors.length === 0, errors.join(" | "));
  return { doc, window };
}

/* ============================================================
 *  场景 2：多轮上下文是否真的带上历史
 * ============================================================ */
async function scenarioMultiTurn() {
  console.log("\n=== 场景 2：多轮上下文 ===");

  let n = 0;
  const { doc, window, errors, calls } = boot(() => {
    n++;
    return Promise.resolve(res([frame(delta("回复" + n)), enc.encode("data: [DONE]\n\n")]));
  });

  type(doc, window, "第一句");
  clickSend(doc, window);
  await sleep(350);

  type(doc, window, "第二句");
  clickSend(doc, window);
  await sleep(350);

  ok("发出了两次请求", calls.length === 2, "n=" + calls.length);
  const m1 = calls[0].body.messages;
  const m2 = calls[1].body.messages;
  ok("第 1 轮只带 1 条", m1.length === 1, JSON.stringify(m1));
  ok("第 2 轮带上历史共 3 条", m2.length === 3, JSON.stringify(m2));
  ok("第 2 轮角色顺序正确",
     m2[0].role === "user" && m2[1].role === "assistant" && m2[2].role === "user",
     JSON.stringify(m2.map((x) => x.role)));
  ok("第 2 轮携带上一轮回复内容", m2[1].content === "回复1", m2[1].content);

  const bubbles = doc.querySelectorAll(".msg.assistant .bubble");
  ok("两轮回复都渲染在界面上", bubbles.length === 2, "n=" + bubbles.length);
  ok("页面无运行时异常", errors.length === 0, errors.join(" | "));
}

/* ============================================================
 *  场景 3：真实后端的返回形状 {event, data}（非流式 JSON）
 * ============================================================ */
async function scenarioRealBackendShape() {
  console.log("\n=== 场景 3：真实后端形状 {event:'chat_response', data:'...'} ===");

  const payload = JSON.stringify({
    event: "chat_response",
    data: "收到，**结果如下**。\n\n| 项 | 值 |\n|---|---|\n| 状态 | 成功 |"
  });

  const { doc, window, errors } = boot(() => Promise.resolve(
    res([enc.encode(payload)], "application/json; charset=utf-8")
  ));

  type(doc, window, "打开相机");
  clickSend(doc, window);
  await sleep(400);

  const bubble = doc.querySelector(".msg.assistant .bubble");
  ok("能解析 {event,data} 形状", !!bubble && bubble.textContent.includes("收到，结果如下"),
     bubble ? bubble.textContent.slice(0, 120) : "null");
  ok("其中的 Markdown 仍被渲染", bubble && bubble.innerHTML.includes("<strong>结果如下</strong>"),
     bubble ? bubble.innerHTML.slice(0, 200) : "null");
  ok("表格被渲染", bubble && bubble.innerHTML.includes("<table>"));
  ok("未误报错误气泡", !doc.querySelector(".error-bubble"));
  ok("页面无运行时异常", errors.length === 0, errors.join(" | "));
}

/* ============================================================
 *  场景 4：HTTP 错误
 * ============================================================ */
async function scenarioHTTPError() {
  console.log("\n=== 场景 4：后端返回 500 ===");

  const { doc, window, errors } = boot(() => Promise.resolve(
    res([enc.encode("Traceback: model not loaded")], "text/plain", 500, "Internal Server Error")
  ));

  type(doc, window, "你好");
  clickSend(doc, window);
  await sleep(350);

  const err = doc.querySelector(".error-bubble");
  ok("出现错误气泡", !!err);
  ok("错误气泡含状态码", err && err.textContent.includes("HTTP 500"), err ? err.textContent.slice(0, 120) : "");
  ok("错误气泡含后端原文", err && err.textContent.includes("model not loaded"));
  ok("错误气泡给出排查提示", err && err.textContent.includes("后端内部错误"));
  ok("页面无运行时异常", errors.length === 0, errors.join(" | "));
}

/* ============================================================
 *  场景 5：网络层失败（跨域/未启动）
 * ============================================================ */
async function scenarioNetworkFail() {
  console.log("\n=== 场景 5：fetch 抛错（跨域或后端未启动）===");

  const { doc, window, errors } = boot(() => Promise.reject(new TypeError("Failed to fetch")));

  type(doc, window, "你好");
  clickSend(doc, window);
  await sleep(350);

  const err = doc.querySelector(".error-bubble");
  ok("出现错误气泡", !!err);
  ok("提示了跨域/未启动两种可能", err && err.textContent.includes("Access-Control-Allow-Origin"));
  ok("页面无运行时异常", errors.length === 0, errors.join(" | "));
}

/* ============================================================
 *  场景 6：停止生成，保留已收到的半截内容
 * ============================================================ */
async function scenarioAbort() {
  console.log("\n=== 场景 6：中途停止生成 ===");

  const { doc, window, errors } = boot((url, init) => Promise.resolve(
    resAbortable([frame(delta("已经收到的前半段")), frame(delta("更多内容"))], init.signal)
  ));

  type(doc, window, "写一段长文");
  clickSend(doc, window);
  await sleep(120);

  const btn = doc.getElementById("btnSend");
  ok("生成中按钮变为停止态", btn.classList.contains("stop"));

  btn.dispatchEvent(new window.MouseEvent("click", { bubbles: true }));
  await sleep(250);

  const bubble = doc.querySelector(".msg.assistant .bubble");
  ok("保留了已收到的部分内容", bubble && bubble.textContent.includes("已经收到的前半段"),
     bubble ? bubble.textContent : "null");
  ok("标注了连接中断", !!doc.querySelector(".interrupted"));
  ok("停止后按钮恢复发送态", !doc.getElementById("btnSend").classList.contains("stop"));
  ok("页面无运行时异常", errors.length === 0, errors.join(" | "));

  // 中断后还应能继续发下一条
  type(doc, window, "再来一次");
  clickSend(doc, window);
  await sleep(200);
  ok("中断后可继续发新消息",
     doc.querySelectorAll(".msg.user").length === 2,
     "n=" + doc.querySelectorAll(".msg.user").length);
}

/* ============================================================
 *  场景 7：多会话与主题
 * ============================================================ */
async function scenarioSessions() {
  console.log("\n=== 场景 7：多会话与主题切换 ===");

  const { doc, window, errors } = boot(() =>
    Promise.resolve(res([frame(delta("ok")), enc.encode("data: [DONE]\n\n")])));

  type(doc, window, "会话A的问题");
  clickSend(doc, window);
  await sleep(300);

  doc.getElementById("btnNew").dispatchEvent(new window.MouseEvent("click", { bubbles: true }));
  await sleep(50);

  ok("新建后侧边栏有 2 个会话", doc.querySelectorAll(".sess").length === 2,
     "n=" + doc.querySelectorAll(".sess").length);
  ok("新建后回到空状态引导页", !!doc.querySelector(".empty"));
  ok("引导页有 3 个示例卡片", doc.querySelectorAll(".prompt-card").length === 3,
     "n=" + doc.querySelectorAll(".prompt-card").length);

  const card = doc.querySelector(".prompt-card");
  card.dispatchEvent(new window.MouseEvent("click", { bubbles: true }));
  ok("点示例卡片填入输入框", doc.getElementById("input").value.length > 0,
     doc.getElementById("input").value.slice(0, 40));

  // 切回第一个会话
  const rows = doc.querySelectorAll(".sess");
  let target = null;
  rows.forEach((r) => { if (r.textContent.includes("会话A的问题")) target = r; });
  ok("历史会话标题可读", !!target);
  target.dispatchEvent(new window.MouseEvent("click", { bubbles: true }));
  await sleep(50);
  ok("切回历史会话后消息恢复", doc.querySelectorAll(".msg.user").length === 1,
     "n=" + doc.querySelectorAll(".msg.user").length);

  // 主题
  const root = doc.documentElement;
  ok("默认浅色主题", root.dataset.theme === "light", root.dataset.theme);
  doc.getElementById("btnTheme").dispatchEvent(new window.MouseEvent("click", { bubbles: true }));
  ok("可切换到深色主题", root.dataset.theme === "dark", root.dataset.theme);
  ok("主题选择已持久化", window.localStorage.getItem("aoi_chat.theme.v1") === "dark",
     window.localStorage.getItem("aoi_chat.theme.v1"));

  ok("页面无运行时异常", errors.length === 0, errors.join(" | "));

  // 刷新恢复
  const saved = window.localStorage.getItem("aoi_chat.sessions.v1");
  const dom2 = new JSDOM(html, {
    url: "http://localhost:5199/",
    runScripts: "dangerously",
    pretendToBeVisual: true,
    beforeParse(w) {
      w.TextDecoder = TextDecoder; w.TextEncoder = TextEncoder;
      w.ReadableStream = ReadableStream; w.Response = Response; w.Headers = Headers;
      w.matchMedia = () => ({ matches: false, addEventListener() {}, removeEventListener() {} });
      w.fetch = () => Promise.resolve(res([enc.encode("data: [DONE]\n\n")]));
      w.localStorage.setItem("aoi_chat.sessions.v1", saved);
      w.localStorage.setItem("aoi_chat.current.v1", JSON.parse(saved)[0].id);
      w.localStorage.setItem("aoi_chat.theme.v1", "dark");
    }
  });
  await sleep(80);
  const d2 = dom2.window.document;
  ok("刷新后历史会话恢复", d2.querySelectorAll(".sess").length === 2,
     "n=" + d2.querySelectorAll(".sess").length);
  ok("刷新后主题恢复", d2.documentElement.dataset.theme === "dark", d2.documentElement.dataset.theme);
}

/* ============================================================ */
(async () => {
  await scenarioStream();
  await scenarioMultiTurn();
  await scenarioRealBackendShape();
  await scenarioHTTPError();
  await scenarioNetworkFail();
  await scenarioAbort();
  await scenarioSessions();

  console.log("\n=============================");
  console.log("PASS=" + pass + "  FAIL=" + fail);
  if (failures.length) console.log("失败项：\n  - " + failures.join("\n  - "));
  process.exit(fail ? 1 : 0);
})().catch((e) => {
  console.error("\n测试脚本自身异常：", e);
  process.exit(2);
});
