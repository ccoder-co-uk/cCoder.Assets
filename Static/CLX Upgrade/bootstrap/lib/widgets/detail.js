class DetailWidget extends Widget {
    // Consumes https://demos.telerik.com/kendo-ui/templates/expressions to build a read only detail view of an object
    // or portion of an object
    constructor(element, args) {
        super(element);
        args = args || {};
        // default configuation for all grid widgets.
        this.detailName = $(element).attr("name");
        this.detailElement = element;
        this.fields = args.fields;
        this.labelTooltip = true;
        this.title = args.title || "Details";

        if (args.hasOwnProperty("header")) {//false || true -> true. Hence hasOwnProperty is required.
            this.header = args.header;
        } else {
            this.header = true;
        }

        this.editable = false;
        this.splits = args.splits;

        if (args.data) {
            this.data = new kendo.observable(args.data);
        }

        this.config = { endpoint: $(this.detailElement).data("endpoint"), odataAppend: "(" + getQueryParameter("Id") + ")" + $(element).data("odataappend") };

        // attach this object to the widget element and add the grid element to prepare for an init call.
        $(this.detailElement).data("widget", this);
    }

    async init() {
        if (!this.template) { this.buildTemplate(); }

        if (this.data) {
            await this.render();
        }
        else {
            let d = await api.get(this.config.endpoint + this.config.odataAppend);
            this.data = new kendo.observable(d);
            this.parseData(this.fields);
            await this.render();
        }
    }

    parseData(meta) {
        $.each(meta, (i, p) => {
            if (p.type === 'date') {
                this.data[p.field] = this.data[p.field] !== null
                    ? new Date(this.data[p.field])
                    : this.data[p.field];
            }
        });
    }

    async render() {
        $(this.detailElement).append(kendo.template(this.template)(this.data));
    }

    computeFields(exclude, callback) {
        let that = this;
        type.fieldsFor(this.config.endpoint, (fields) => {
            that.fields = fields.filter(f => exclude.IndexOf(f.field) < 0);
            if (callback) { callback(); }
        });
    }

    buildTemplate() {

        let build = (that) => {
            const title = kendo.template(that.title);
            const fields = that.fields.map((meta) => ({ meta, render: kendo.template(that.fieldValueExpression(meta.field)) }));
            that.template = function (data) {
                let html = that.header ? '<h3>' + title(data) + '</h3>' : '';
                if (that.toolbar) html += "<div class='k-header k-grid-toolbar'>" + that.toolbar + '</div>';
                html += "<div name='details'><ul class='fieldList'>";
                fields.forEach(({ meta, render }) => {
                    if (that.splits && that.splits.indexOf(meta.field) > -1) html += "</ul><ul class='fieldList'>";
                    html += "<li name='" + that.config.endpoint + '/' + meta.field + "'><label title='" + meta.description + "' for='" + meta.field + "'>" + meta.title + "</label><div class='value'>" + render(data) + '</div></li>';
                });
                return html + '</ul></div>';
            };
        };

        if (!this.fields) {
            this.computeFields(null, () => { build(this); });
        }
        else { build(this); }
    }

    fieldValueExpression(fieldName) { /* Intentional */ }
}