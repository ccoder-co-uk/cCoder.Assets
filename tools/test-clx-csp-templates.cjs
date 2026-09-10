const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.join(__dirname, '../Data/demo.dev.localhost/Common Cache');
const encode = value => String(value ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
function context() {
  const c = vm.createContext({ kendo: { htmlEncode: encode, template(value) {
    if (typeof value === 'function') return value;
    if (/#[:=]/.test(value)) throw new EvalError('String template requires dynamic compilation');
    return () => value;
  } } }, { codeGeneration: { strings: false, wasm: false } });
  vm.runInContext('class Widget {}', c);
  return c;
}
function load(c, name) {
  const file = name.slice(8);
  const source = fs.readFileSync(path.join(__dirname, '../Static/CLX Upgrade/bootstrap/lib/widgets', file[0].toLowerCase() + file.slice(1) + '.js'), 'utf8');
  vm.runInContext(source, c);
}
test('grid commands render functional links and HTML alongside static buttons under CSP', () => {
  const c = context(); load(c, 'Widgets.Grid');
  const render = vm.runInContext(`GridWidget.prototype.commandColumn.call({commands:[
    {name:'view',href:row=>'/record/'+row.Id,icon:'view',text:'View'},
    {template:row=>'<b>'+kendo.htmlEncode(row.Name)+'</b>'},
    {name:'save',icon:'save',text:'Save'}]})`, c);
  const html = c.kendo.template(render)({Id:42,Name:'A & <B>'});
  assert.match(html, /href='\/record\/42'/);
  assert.match(html, /<b>A &amp; &lt;B&gt;<\/b>/);
  assert.match(html, /name="save"/);
  assert.doesNotMatch(html, /function|=>/);
});
test('detail views render functional titles and fields without string compilation', () => {
  const c = context(); load(c, 'Widgets.Detail');
  const detail = vm.runInContext(`({header:true,title:row=>'Record '+row.Id,fields:[{field:'Name',title:'Name',description:'Name'}],config:{endpoint:'Example'},fieldValueExpression:()=>row=>kendo.htmlEncode(row.Name)})`, c);
  vm.runInContext('DetailWidget.prototype.buildTemplate', c).call(detail);
  const html = c.kendo.template(detail.template)({Id:42,Name:'A & <B>'});
  assert.match(html, /<h3>Record 42<\/h3>/);
  assert.match(html, /A &amp; &lt;B&gt;/);
});
test('folder tree returns a rendering function and encodes folder names', () => {
  const c = context();
  const source = JSON.parse(fs.readFileSync(path.join(root, 'DocumentManagement/Components/FolderActions.json'), 'utf8'));
  vm.runInContext(source.Script, c);
  const html = c.kendo.template(c.FolderActions.folderTreeTemplate())({item:{text:'A & <B>'}});
  assert.match(html, /A &amp; &lt;B&gt;/);
  assert.doesNotMatch(html, /#[:=]/);
});
function builder(name, key) {
  const c = context();
  c.$ = () => ({ ready() {} }); c.document = {};
  c.type = { dateFormat: 'yyyy-MM-dd' }; c.my = { formatCurrency: value => 'EUR ' + value };
  c.kendo.toString = value => value instanceof Date ? value.toISOString().slice(0,10) : String(value);
  c.JSLINQ = values => ({ Select: fn => ({ ToArray: () => values.map(fn) }) });
  const source = JSON.parse(fs.readFileSync(path.join(root, key, 'Components', name + '.json'), 'utf8'));
  vm.runInContext(source.Script.replace(/\[meta\[[^\]]+\]\]/g, '0'), c);
  return c;
}
test('query-builder default columns render with functions under CSP', () => {
  const c = builder('QueryBuilder', 'DocumentManagement');
  const col = { Name:'Name', Type:'string' };
  col.TemplateSource = c.QueryBuilder.buildDefaultTemplateSourceFor(col);
  const result = c.QueryBuilder.buildColumns([col])[0];
  assert.equal(typeof result.template, 'function');
  assert.equal(result.template({Name:'A & <B>'}), 'A &amp; &lt;B&gt;');
});
test('grid-builder previews and generated columns preserve functions and ordering', () => {
  const c = builder('GridBuilder', 'ContentManagement');
  const cols = [{name:'Name',title:'Name',order:2,template:'#: Name #',templateType:''}, {name:'Amount',title:'Amount',order:1,template:'#:my.formatCurrency(Amount)#',templateType:'currency'}];
  const preview = c.GridBuilder.columnfy(cols.map(x=>({...x})), false);
  assert.equal(typeof preview[0].template, 'function');
  assert.equal(preview[0].template({Amount:12}), 'EUR 12');
  const generated = vm.runInContext('(' + c.GridBuilder.columnSource(cols) + ')', c);
  assert.equal(generated[0].field, 'Amount');
  assert.equal(generated[1].template({Name:'A & <B>'}), 'A &amp; &lt;B&gt;');
});
