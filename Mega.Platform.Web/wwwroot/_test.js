// 单元测试：Markdown 渲染 / 响应体解析 / 代码着色 / SSE 分块解析
// 运行：node _test.js
const fs = require('fs');
const path = require('path');
if (!fs.existsSync(path.join(__dirname, '_lib.js'))) require('./_buildlib.js');

const { renderMarkdown, extractFromObject, highlightCode, createSSEParser } = require('./_lib.js');

let pass = 0, fail = 0;
function ok(name, cond, extra) {
  if (cond) { pass++; console.log('  PASS  ' + name); }
  else { fail++; console.log('  FAIL  ' + name + (extra ? '\n        -> ' + extra : '')); }
}

console.log('\n=== A. Markdown 渲染 ===');

const md1 = '# 标题一\n\n这是**粗体**和*斜体*，还有行内 `code()`。\n\n- 第一项\n- 第二项\n';
const h1 = renderMarkdown(md1);
ok('标题渲染', h1.includes('<h1>标题一</h1>'), h1);
ok('粗体渲染', h1.includes('<strong>粗体</strong>'), h1);
ok('行内代码', h1.includes('<code class="inline-code">code()</code>'), h1);
ok('无序列表', h1.includes('<ul><li>第一项</li><li>第二项</li></ul>'), h1);

const md2 = '```python\ndef f(x):\n    return x + 1\n```';
const h2 = renderMarkdown(md2);
ok('代码块容器', h2.includes('class="code-block"'), h2);
ok('语言标签', h2.includes('>python<'), h2);
ok('关键字着色', h2.includes('tok-k">def</span>'), h2);
ok('复制按钮', h2.includes('data-copy="1"'), h2);

const md3 = '```python\ndef f(x):\n    return x';
const h3 = renderMarkdown(md3);
ok('未闭合代码块标记流式', h3.includes('data-streaming="1"'), h3);
ok('未闭合代码块仍渲染', h3.includes('return') , h3);

const md4 = '- 一级\n  - 二级\n  - 二级B\n- 一级2';
const h4 = renderMarkdown(md4);
ok('嵌套列表', (h4.match(/<ul>/g) || []).length === 2, h4);
ok('嵌套内容正确', h4.includes('<li>一级<ul><li>二级</li><li>二级B</li></ul></li>'), h4);

const md5 = '| 模型 | 场景 |\n|---|---|\n| chat | 通用 |\n| reasoner | 推理 |';
const h5 = renderMarkdown(md5);
ok('表格 thead', h5.includes('<thead><tr><th>模型</th><th>场景</th></tr></thead>'), h5);
ok('表格 tbody 行数', (h5.match(/<tr>/g) || []).length === 3, h5);

const md6 = '> 引用第一行\n> 引用第二行';
const h6 = renderMarkdown(md6);
ok('引用块', h6.includes('<blockquote>') && h6.includes('引用第二行'), h6);

const xss = '正常文字 <script>alert(1)</script> 与 <img src=x onerror=alert(2)>';
const hx = renderMarkdown(xss);
ok('XSS 被转义', !hx.includes('<script>') && !hx.includes('<img'), hx);
ok('XSS 以实体呈现', hx.includes('&lt;script&gt;'), hx);

const md7 = '行内 `**不应加粗**` 结束';
const h7 = renderMarkdown(md7);
ok('行内代码内不做强调', h7.includes('<code class="inline-code">**不应加粗**</code>'), h7);
ok('行内代码外无 strong', !h7.includes('<strong>'), h7);

const md8 = '正文第一行\n正文第二行';
const h8 = renderMarkdown(md8);
ok('段落内换行变 br', h8.includes('正文第一行<br>正文第二行'), h8);

const md9 = 'snake_case_name 不应变斜体';
const h9 = renderMarkdown(md9);
ok('下划线变量名不被斜体', !h9.includes('<em>'), h9);

const md10 = '1. 甲\n2. 乙';
const h10 = renderMarkdown(md10);
ok('有序列表', h10.includes('<ol><li>甲</li><li>乙</li></ol>'), h10);

ok('空输入安全', renderMarkdown('') === '');

console.log('\n=== B. 响应体多形状解析 ===');

const cases = [
  ['OpenAI 流式 delta', { choices: [{ delta: { content: '你好' } }] }, '你好', ''],
  ['OpenAI 非流式 message', { choices: [{ message: { content: 'hi' } }] }, 'hi', ''],
  ['reasoning_content', { choices: [{ delta: { reasoning_content: '思考' } }] }, '', '思考'],
  ['reasoning 别名', { choices: [{ delta: { reasoning: '想' } }] }, '', '想'],
  ['reasoner 同时给两者', { choices: [{ delta: { reasoning_content: 'R', content: 'C' } }] }, 'C', 'R'],
  ['顶层 content', { content: 'x' }, 'x', ''],
  ['顶层 text', { text: 'y' }, 'y', ''],
  ['顶层 reply', { reply: 'z' }, 'z', ''],
  ['顶层 answer', { answer: 'a' }, 'a', ''],
  ['顶层 response', { response: 'b' }, 'b', ''],
  ['choices[0].text', { choices: [{ text: 't' }] }, 't', ''],
  ['包装 data.content', { data: { content: 'w' } }, 'w', ''],
  ['包装 data.choices', { data: { choices: [{ delta: { content: 'q' } }] } }, 'q', ''],
  ['字符串直传', 'raw', 'raw', ''],
  ['空对象', {}, '', ''],
  ['null 安全', null, '', '']
];
cases.forEach(([name, input, expC, expR]) => {
  const r = extractFromObject(input, 0);
  ok(name, r.content === expC && r.reasoning === expR,
     'got ' + JSON.stringify(r) + ' expect {' + expC + ',' + expR + '}');
});

console.log('\n=== C. 代码着色安全性 ===');
const hl = highlightCode('const a = "<b>hi</b>"; // 注释', 'javascript');
ok('着色输出转义尖括号', !hl.includes('<b>hi</b>'), hl);
ok('字符串着色', hl.includes('tok-s'), hl);
ok('注释着色', hl.includes('tok-c'), hl);
const hl2 = highlightCode('# 注释\nx = "s"', 'python');
ok('Python 井号注释', hl2.includes('tok-c'), hl2);
const hl3 = highlightCode('SELECT * FROM t -- 注释', 'sql');
ok('SQL 短横注释', hl3.includes('tok-c'), hl3);
const hl4 = highlightCode('const a = 1', 'unknownlang');
ok('未知语言不崩', typeof hl4 === 'string' && hl4.length > 0, hl4);
const hl5 = highlightCode('x = 42', 'python');
ok('数字着色 tok-n', hl5.includes('tok-n'), hl5);
const hl6 = highlightCode('def f(x):\n    return x + 1', 'python');
ok('Python 关键字着色', hl6.includes('tok-k">def</span>') && hl6.includes('tok-k">return</span>'), hl6);
ok('Python 数字用 tok-n', hl6.includes('<span class="tok-n">1</span>'), hl6);
ok('Python 无 tok-s 误判', !hl6.includes('tok-s'), hl6);
const hl7 = highlightCode('def f():\n    # 注释\n    return "s"', 'python');
ok('Python 注释与字符串各归其位',
   hl7.includes('tok-c"># 注释</span>') && hl7.includes('tok-s">&quot;s&quot;</span>'), hl7);

console.log('\n=== D. SSE 分块解析（含 TCP 半行切分）===');

function collect(chunks) {
  const got = [];
  const p = createSSEParser((pl) => got.push(pl));
  chunks.forEach((c) => p.push(c));
  const fin = p.flush();
  return { got, fin };
}

let r1 = collect(['data: {"a":1}\n\ndata: {"b":2}\n\n']);
ok('基本两帧', r1.got.length === 2 && r1.got[1] === '{"b":2}', JSON.stringify(r1.got));
ok('识别为 SSE', r1.fin.sawSSE === true);

// 关键用例：一行 JSON 被切成三段
let r2 = collect(['data: {"cho', 'ices":[{"delta"', ':{"content":"你好"}}]}\n']);
ok('跨块半行重组', r2.got.length === 1 && r2.got[0] === '{"choices":[{"delta":{"content":"你好"}}]}', JSON.stringify(r2.got));

// CRLF
let r3 = collect(['data: {"x":1}\r\n']);
ok('CRLF 兼容', r3.got.length === 1 && r3.got[0] === '{"x":1}', JSON.stringify(r3.got));

// [DONE] 原样透传（由上层判断）
let r4 = collect(['data: [DONE]\n']);
ok('[DONE] 透传给上层', r4.got.length === 1 && r4.got[0] === '[DONE]', JSON.stringify(r4.got));

// 心跳与 event 行应被忽略
let r5 = collect([': ping\n', 'event: message\n', 'data: {"y":2}\n', '\n']);
ok('忽略心跳与 event 行', r5.got.length === 1 && r5.got[0] === '{"y":2}', JSON.stringify(r5.got));

// 末帧没有换行符，必须靠 flush 收尾
let r6 = collect(['data: {"last":true}']);
ok('无结尾换行靠 flush 兜住', r6.got.length === 1 && r6.got[0] === '{"last":true}', JSON.stringify(r6.got));

// 非 SSE 响应：不应误判
let r7 = collect(['{"content":"一次性返回"}']);
ok('非 SSE 不误判', r7.got.length === 0 && r7.fin.sawSSE === false, JSON.stringify(r7));
ok('非 SSE 原文保留供兜底', r7.fin.raw === '{"content":"一次性返回"}', JSON.stringify(r7.fin.raw));

// 逐字节喂入（最极端的切分）
const full = 'data: {"choices":[{"delta":{"content":"逐字节"}}]}\n\ndata: [DONE]\n';
let r8 = collect(full.split(''));
ok('逐字节喂入仍正确', r8.got.length === 2 && r8.got[0] === '{"choices":[{"delta":{"content":"逐字节"}}]}', JSON.stringify(r8.got));

// 与真实调用链拼接：切块 → 解析 → 抽字段
const stream = 'data: {"choices":[{"delta":{"reasoning_content":"让我想想"}}]}\n\ndata: {"choices":[{"delta":{"content":"答案是"}}]}\n\ndata: {"choices":[{"delta":{"content":"42"}}]}\n\ndata: [DONE]\n';
let acc = { content: '', reasoning: '' };
const p9 = createSSEParser((pl) => {
  if (pl === '[DONE]') return;
  const piece = extractFromObject(JSON.parse(pl), 0);
  acc.content += piece.content;
  acc.reasoning += piece.reasoning;
});
stream.match(/[\s\S]{1,17}/g).forEach((c) => p9.push(c));
p9.flush();
ok('端到端拼装正文', acc.content === '答案是42', JSON.stringify(acc));
ok('端到端拼装思维链', acc.reasoning === '让我想想', JSON.stringify(acc));

console.log('\n-----------------------------');
console.log('PASS=' + pass + '  FAIL=' + fail);
process.exit(fail ? 1 : 0);
