const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const source = JSON.parse(fs.readFileSync(path.join(__dirname, '../Data/demo.dev.localhost/Common Cache/B2B/Components/BucketedCompanyManagement.json'), 'utf8')).Script;

function component(app) {
  const grids = [], requests = [], pending = [];
  const jq = () => ({ off() { return this; }, on() { return this; }, first() { return this; }, html() { return ''; } });
  const sandbox = { session: { app }, $: jq, type: { dateFormat: 'yyyy-MM-dd' },
    model: { getDatasource: async config => { requests.push(config); return {}; } },
    GridWidget: class { constructor() { this.commands = []; grids.push(this); } async init() {} },
    Dialog: class { constructor() { this.events = {}; } init(callback) { pending.push(callback()); } }
  };
  vm.createContext(sandbox);
  vm.runInContext(source.replace(/\[meta\[[^\]]+\]\]/g, '{}'), sandbox);
  return { api: sandbox.BucketedCompanyManagement, grids, requests, pending };
}

test('Portal Admin can open company grids without an app source-system restriction', async () => {
  const app = { Config: { B2B: {} } };
  const c = component(app);
  await c.api.setupGrid(app, {}, { Id: 21 });
  const reference = c.grids[0].columns.find(x => x.field === 'Reference');
  assert.equal(reference.template({ References: [{ Id: 'GlobalTax' }, { Id: 'OtherSystem' }] }), 'GlobalTax,OtherSystem');
  await c.api.selectCompanyToAdd({ preventDefault() {} }, app, {}, 21);
  await Promise.all(c.pending);
  assert.equal(c.requests[1].endpoint, 'B2B/Company');
  assert.equal(c.requests[1].odataAppend, '?$expand=References');
});

test('App administration retains source-system filtering', async () => {
  const app = { Config: { B2B: { SourceSystem: 'Demo,Second' } } };
  const c = component(app);
  await c.api.setupGrid(app, {}, { Id: 21 });
  const reference = c.grids[0].columns.find(x => x.field === 'Reference');
  assert.equal(reference.template({ References: [{ Id: 'GlobalTax' }, { Id: 'Demo' }, { Id: 'Other' }] }), 'GlobalTax,Demo');
  await c.api.selectCompanyToAdd({ preventDefault() {} }, app, {}, 21);
  await Promise.all(c.pending);
  assert.equal(c.requests[1].odataAppend, "/FromSystem(SystemId='Demo,Second')");
});
