// 从 index.html 里抽出纯函数，生成可被 node 直接测试的 _lib.js
// 用法：node _buildlib.js  （由 _test.js 自动调用，也可单独跑）
const fs = require('fs');
const path = require('path');
const dir = __dirname;
const html = fs.readFileSync(path.join(dir, 'index.html'), 'utf8');
const m = html.match(/<script>([\s\S]*?)<\/script>/);
if (!m) throw new Error('未找到内联脚本');
const src = m[1];
fs.writeFileSync(path.join(dir, '_check.js'), src);

function grab(name) {
  const a = src.indexOf('function ' + name);
  if (a < 0) throw new Error('未找到函数: ' + name);
  let d = 0, j = src.indexOf('{', a);
  for (; j < src.length; j++) {
    if (src[j] === '{') d++;
    else if (src[j] === '}') { d--; if (!d) break; }
  }
  return src.slice(a, j + 1);
}

const s = src.indexOf('const KEYWORD_SRC');
const e = src.indexOf('*  7. 消息渲染');
const block = src.slice(s, src.lastIndexOf('/*', e));

const out = [
  grab('escapeHtml'),
  block,
  grab('extractFromObject'),
  grab('createSSEParser'),
  'module.exports={renderMarkdown,extractFromObject,highlightCode,escapeHtml,createSSEParser};'
].join('\n');

fs.writeFileSync(path.join(dir, '_lib.js'), out);
console.log('已生成 _lib.js (' + out.length + ' bytes)');
