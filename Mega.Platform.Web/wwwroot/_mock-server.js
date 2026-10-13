/**
 * 本地桩服务 —— 仅用于前端自测，不是你的真实后端，可直接删除。
 *
 * 作用：在 http://localhost:5199 上模拟一个 OpenAI 兼容的流式接口，
 *       同时把 index.html 一起托管（同源，便于验证）。
 *
 * 启动：node _mock-server.js
 * 然后浏览器打开：http://localhost:5199
 *
 * 注意：端口故意避开 5114，免得和你的真实后端打架。
 *       要用桩服务测页面时，把 index.html 里的 CONFIG.endpoint 临时改成
 *       "http://localhost:5199/chat" 即可。
 */
const http = require("http");
const fs = require("fs");
const path = require("path");

const PORT = Number(process.env.PORT) || 5199;
const HTML = path.join(__dirname, "index.html");

// 按模型返回不同风格的内容：reasoner 会先推思维链
const REPLY_CHAT = [
  "好的，我按你的问题拆开说。\n\n",
  "## 一、核心矛盾\n\n",
  "过检（Overkill）和漏检（Underkill）本质上是**同一个阈值的一体两面**：\n\n",
  "| 调高灵敏度 | 调低灵敏度 |\n|---|---|\n| 漏检下降 | 漏检上升 |\n| 过检上升 | 过检下降 |\n\n",
  "## 二、可落地的判据\n\n",
  "1. 先定 **逃逸成本**，再定阈值\n",
  "2. 按缺陷类型分级，而不是全局统一\n",
  "3. 用 Pareto 找 Top-3 缺陷族单独调参\n\n",
  "下面是一段示例代码：\n\n",
  "```python\n",
  "def yield_impact(overkill, underkill, w_o=0.3, w_u=0.7):\n",
  "    \"\"\"加权良率损失，w_u 通常远大于 w_o\"\"\"\n",
  "    return overkill * w_o + underkill * w_u\n",
  "```\n\n",
  "最后用引用收个尾。\n\n",
  "> 判据的选择永远服务于最终良率，而不是设备本身的指标好看。\n"
].join("");

const REASONING = [
  "用户在问 AOI 的过检与漏检权衡。\n",
  "我先要把两者的定义对齐：过检是把好品判为坏品，漏检是把坏品判为好品。\n",
  "然后要给出可量化的判据，而不是泛泛而谈。\n",
  "最后要提醒权重不对称这一点，这是实际产线里最容易被忽略的。\n"
].join("");

function sse(res, obj) {
  res.write("data: " + JSON.stringify(obj) + "\n\n");
}
function delta(content, reasoning) {
  const d = {};
  if (reasoning) d.reasoning_content = reasoning;
  if (content) d.content = content;
  return { choices: [{ index: 0, delta: d }] };
}

const server = http.createServer((req, res) => {
  // 预检
  if (req.method === "OPTIONS") {
    res.writeHead(204, {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "POST, OPTIONS",
      "Access-Control-Allow-Headers": "Content-Type, Authorization"
    });
    return res.end();
  }

  // 顺带托管页面（同源）
  if (req.method === "GET" && (req.url === "/" || req.url === "/index.html")) {
    fs.readFile(HTML, (err, buf) => {
      if (err) { res.writeHead(500); return res.end("index.html 读取失败"); }
      res.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
      res.end(buf);
    });
    return;
  }

  if (req.method !== "POST" || req.url !== "/chat") {
    res.writeHead(404, { "Content-Type": "application/json" });
    return res.end(JSON.stringify({ error: "仅支持 POST /chat" }));
  }

  let body = "";
  req.on("data", (c) => { body += c; });
  req.on("end", () => {
    let payload = {};
    try { payload = JSON.parse(body); } catch (_) {}
    const model = payload.model || "deepseek-chat";
    const isReasoner = String(model).indexOf("reasoner") >= 0;
    const wantStream = payload.stream !== false;

    console.log("[" + new Date().toLocaleTimeString() + "] " + model +
                (wantStream ? " (stream)" : " (json)") +
                " ← " + JSON.stringify((payload.messages || []).slice(-1)[0] || {}).slice(0, 90));

    // 非流式
    if (!wantStream) {
      res.writeHead(200, { "Content-Type": "application/json; charset=utf-8" });
      return res.end(JSON.stringify({
        choices: [{ message: { role: "assistant", content: REPLY_CHAT } }]
      }));
    }

    res.writeHead(200, {
      "Content-Type": "text/event-stream; charset=utf-8",
      "Cache-Control": "no-cache",
      "Connection": "keep-alive",
      "Access-Control-Allow-Origin": "*",
      "X-Accel-Buffering": "no"
    });

    const steps = [];
    if (isReasoner) {
      // 思维链按小块推，模拟真实推理模型的延迟
      for (const seg of REASONING.match(/[\s\S]{1,12}/g) || []) steps.push({ r: seg, delay: 45 });
    }
    for (const seg of REPLY_CHAT.match(/[\s\S]{1,9}/g) || []) steps.push({ c: seg, delay: 28 });

    let i = 0;
    const tick = () => {
      if (i >= steps.length) {
        res.write("data: [DONE]\n\n");
        return res.end();
      }
      const st = steps[i++];
      sse(res, delta(st.c, st.r));
      setTimeout(tick, st.delay);
    };
    tick();
  });
});

server.listen(PORT, () => {
  console.log("桩服务已启动");
  console.log("  页面：http://localhost:" + PORT);
  console.log("  接口：http://localhost:" + PORT + "/chat");
});
