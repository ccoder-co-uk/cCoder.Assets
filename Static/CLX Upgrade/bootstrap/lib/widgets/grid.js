class GridWidget extends Widget {
    constructor(element, dataSource) {
        super(element);

        // default configuation for all grid widgets.
        this.gridName = $(element).attr("name");
        $(element).prepend("<div name='" + this.gridName + "Grid'></div>");
        this.gridElement = $("[name=" + this.gridName + "Grid]", $(element));
        this.kendoDataSource = dataSource;
        this.toolbar = [];
        this.commands = [];
        this.searchable = false;
        this.editable = true;
        this.commandWidth = 100;
        this.sortable = true;
        this.reorderable = true;
        this.persistSelection = false;
        this.selectColumns = false;
        this.change = function (e) { /* to avoid exceptions when called */ };
        this.resizable = true;
        this.groupable = true;
        this.headerTooltip = true;
        this.exports = false;
        this.scrollable = true;
        this.resizable = {
            rows: true
        };
        this.rowResize = () => { };

        this.pageable = {
            refresh: true,
            pageSizes: [10, 20, 50, 100, 200, 500, 1000],
            numeric: true
        };

        this.endpoint = $(this.gridElement).data("endpoint");
        this.odataAppend = $(this.gridElement).data("odataappend");

        if (session.app.Config.hasOwnProperty("Components") && session.app.Config.Components.hasOwnProperty("Grids") &&
            session.app.Config.Components.Grids.hasOwnProperty("Details")) {

            this.viewCommand = (session.app.Config.Components.Grids.Details.indexOf("Link") !== -1) || false;
            this.expandView = (session.app.Config.Components.Grids.Details.indexOf("Expand") !== -1) || false;
            this.viewDialogCommand = (session.app.Config.Components.Grids.Details.indexOf("Popup") !== -1) || false;
        } else {
            this.viewCommand = true;
            this.expandView = true;
            this.viewDialogCommand = true;
        }
        // if we have a detail template, expansion by convention is supported, else remove expansion functionality.
        let detailTemplate = $("script[name=details]", this.element);

        if (detailTemplate.length === 1) {
            this.detailTemplate = kendo.template($("script[name=details]", this.element).html());
        }
        else {
            delete this.detailExpand;
        }

        // attach this object to the widget element and add the grid element to prepare for an init call.
        $(this.gridElement).data("widget", this);
    }

    dataItem(row) {
        return this.kendoObject.dataItem(row);
    }

    refresh() {
        this.kendoObject.dataSource.read();
        this.kendoObject.refresh();
    }

    dataSource() {
        return this.kendoObject.dataSource;
    }

    clearSelection() {
        this.kendoObject.clearSelection();
    }

    select() {
        let items = [];
        let grid = this;
        this.kendoObject.select().each(function (i) {
            items.push(grid.dataItem(this))
        });
        return items;
    }

    setColumns(columns) {
        this.kendoObject.setOptions({ columns: columns });
        this.postInit();
    }

    async buildConfig() {
        if (this.groupable) {
            this.groupable = {
                messages: {
                    empty: (await api.getResource("Default", "Grouping", session.culture)).Description
                }
            };
        }

        this.config = {
            endpoint: this.endpoint,
            odataAppend: this.odataAppend,
            columns: this.columns,
            scrollable: this.scrollable,
            pageable: this.pageable,
            editable: this.editable,
            sortable: this.sortable ? { mode: "multiple", allowUnsort: true, showIndexes: true } : false,
            reorderable: this.reorderable,
            change: this.change,
            resizable: this.resizable,
            rowResize: this.rowResize,
            filterable: this.filterable,
            groupable: this.groupable,
            dataBound: this.dataBound,
            detailTemplate: this.detailTemplate,
            detailExpand: this.detailExpand,
            search: this.search,
            sort: this.sort,
            persistSelection: this.persistSelection
        };

        if (this.toolbar.length) {
            this.config.toolbar = this.toolbar;
        }

        this.config.dataSource = this.kendoDataSource || (await model.getDatasource(this.config));
        return this.config;
    }

    async init() {
        let that = this;

        if (that.filterable) {
            await that.setupFilteringOperators.apply(that);
        }

        if (!that.columns) {
            that.columns = model.columnsFor(that.endpoint);
        }

        if (that.commands.length > 0) {
            that.columns.push({ width: that.commands.length * that.commandWidth, template: that.commandColumn() });
        }

        for (let i in that.columns) {
            that.enableMultiselectFilteringOn(that.columns[i]);
        }

        if (that.searchable && that.search == null) {
            that.search = {
                fields: that.columns
                    .filter(column => column.field && (column.type === "string" || column.Type === "string"))
                    .map(column => column.field)
            };
        }

        await that.buildConfig.apply(that);

        if (that.config.toolbar != null && !Array.isArray(that.config.toolbar)) {
            that.config.toolbar = [{ template: that.config.toolbar }];
        }

        if (that.searchable) {
            if (that.config.toolbar == null) { that.config.toolbar = []; }
            that.config.toolbar.push({ name: "search" });
        }

        if (that.exports) {
            that.config.enableExport = true;

            that.config.dataSource.setExportUrls = function () {
                that.generateExportLinks.apply(that);
            };

            let exportText = (await api.getResource("Default", "Export", session.culture)).DisplayName;

            if (that.config.toolbar == null) {
                that.config.toolbar = [];
            }

            that.config.toolbar.unshift({
                template: "<button class='btn btn-sm btn-primary' name='exportButton'><span class='k-icon k-i-download'></span>" + exportText + "</button>"
            });
        }

        if (that.selectColumns) {
            let selectColumnsText = (await api.getResource("Default", "selectcolumns", session.culture)).DisplayName;

            if (that.config.toolbar == null) {
                that.config.toolbar = [];
            }

            that.config.toolbar.push({
                template: "<button name='selectColumnsButton'><span class='k-icon k-i-download'></span>" + selectColumnsText + "</button>"
            });
        }

        // initialize the kendo grid
        that.kendoObject = that.gridElement.kendoGrid(that.config).data("kendoGrid");
        this.postInit();
    }
    
    postInit() {
        let that = this;

        if (that.searchable) {
            let searchInput = $(".k-grid-search input", that.gridElement);
            let searchName = (that.gridName || "grid").replace(/[^a-zA-Z0-9_-]/g, "") + "-grid-search";

            searchInput
                .attr("name", searchName)
                .attr("type", "search")
                .attr("autocomplete", "off")
                .attr("autocapitalize", "none")
                .attr("spellcheck", "false")
                .attr("data-1p-ignore", "true")
                .attr("data-lpignore", "true")
                .prop("readonly", true)
                .one("focus pointerdown", function () {
                    $(this).prop("readonly", false);
                });

            // Credential managers can populate the generated Kendo input before
            // GridWidget gets a chance to identify it as a search control.
            if (searchInput.val()) {
                searchInput.val("").trigger("input");
            }
        }

        if (that.headerTooltip) {
            let gridHead = that.kendoObject.thead;
            let cells = gridHead.find("th");

            $(cells).each(async function (index) {

                let fieldName = $(this).attr("data-field");

                if (that.columns && that.columns.filter(c => c.field == fieldName).length > 0) {
                    let matchingColumn = that.columns.filter(c => c.field == fieldName)[0];

                    if (matchingColumn.description) {
                        $(this).attr("title", matchingColumn.description);
                    }
                }
            });
        }

        if (that.selectColumns) { that.generateSelectColumns(); }
        if (that.exports) { that.generateExportLinks(); }
    }

    async setupFilteringOperators() {
        this.filterable = {
            operators: {
                extra: false,
                number: {
                    gte: (await api.getResource("Default", "greaterthan", session.culture)).DisplayName,
                    lte: (await api.getResource("Default", "lessthan", session.culture)).DisplayName,
                    eq: (await api.getResource("Default", "isequalto", session.culture)).DisplayName,
                    neq: (await api.getResource("Default", "isnotequalto", session.culture)).DisplayName
                },
                string: {
                    contains: (await api.getResource("Default", "contains", session.culture)).DisplayName,
                    startswith: (await api.getResource("Default", "startswith", session.culture)).DisplayName,
                    eq: (await api.getResource("Default", "isequalto", session.culture)).DisplayName,
                    neq: (await api.getResource("Default", "isnotequalto", session.culture)).DisplayName
                },
                date: {
                    gte: (await api.getResource("Default", "fromdate", session.culture)).DisplayName,
                    lte: (await api.getResource("Default", "todate", session.culture)).DisplayName
                }
            },
            messages: {
                selectedItemsFormat: (await api.getResource("Default", "selecteditemsformat", session.culture)).DisplayName
            }
        };

        this.columns.filter(c => c.type === "date" && (c.filterable === null || c.filterable === undefined)).forEach(c => {
            c.filterable = {
                ui: (element) => element.kendoDatePicker({ format: type.dateFormat })
            };
        });
    }

    enableMultiselectFilteringOn(col) {
        if (col.values) {
            col.filterable = {
                multi: true,
                dataSource: col.values,
                itemTemplate: function (e) {
                    return function (data) {
                        return (e.field == "all")
                            ? "<li><input type='checkbox' name='" + e.field + "' value='all'/><span>All</span></li>"
                            : "<li><input type='checkbox' name='" + e.field + "' value='" + data.id + "'/><span>" + data.name + "</span></li>";
                    };
                }
            };
        }
    }

    async generateSelectColumns() {
        $("[name=selectColumnsButton]", this.gridElement).on("click", selectColumns);
    }

    async selectColumns(e) {

        let grid = this;
        let selectText = (await api.getResource("Default", "selectcolumns", session.culture)).DisplayName;
        let resourceKey = $(grid.gridElement).closest(".component").attr("data-resource-key");

        let selectColumnDialog = new Dialog({
            title: selectText,
            width: 400,
            height: 800
        });

        let columnsGrid = null;

        selectColumnDialog.template = "<div name='selectColumnGrid' style='width:400px;height:700px;'></div><hr><div class='value'><button name='select'>" + selectText + "</button></div>";

        selectColumnDialog.events.select = async (e) => {
            let selectedColumns = [];
            let selected = columnsGrid.select();

            for (let s in selected) {
                let text = (await api.getResource(resourceKey, s.Name, session.culture))
                    .ShortDisplayName;

                selectedColumns.push({
                    field: s.Name,
                    type: s.Type,
                    title: text,
                    format: getColumnFormat(s.Type)
                });
            }

            if (grid.commands.length > 0) {
                selectedColumns.push({
                    width: grid.commands.length * grid.commandWidth,
                    template: grid.commandColumn()
                });
            }

            for (let i in grid.columns) {
                grid.enableMultiselectFilteringOn(grid.columns[i]);
            }

            grid.setColumns(selectedColumns);
            selectColumnDialog.events.close();
        };

        selectColumnDialog.init(initSelectColumnsDialog);
    }

    getColumnFormat(colType) {

        if (s.Type === "date") {
            return "{0: " + type.dateFormat + "}";
        }

        if (s.Type === "number") {
            return "{0: " + type.moneyFormat + "}";
        }

        return null;
    }

    async initSelectColumnsDialog() {

        let fields = (await api.getType(grid.config.endpoint)).Properties.filter(r => r.Type !== 'array');

        let columnsGrid = new GridWidget($("[name=selectColumnGrid]"), {
            data: fields
        });

        columnsGrid.sortable = true;
        columnsGrid.filterable = false;
        columnsGrid.pageable = false;
        columnsGrid.groupable = false;

        columnsGrid.columns = [
            { width: 50, selectable: true },
            { field: "Name" }
        ];

        await columnsGrid.init();

        $("tbody > tr", columnsGrid.gridElement).each(function () {
            let uid = $(this).attr("data-uid");
            let dataItem = columnsGrid.dataSource().getByUid(uid);

            if (grid.columns.filter(r => r.field == dataItem.Name).length > 0) {
                columnsGrid.kendoObject.select($(this));
            }
        });
    }

    async generateExportLinks() {
        if (this.dataSource || this.config.dataSource) {
            $("[name=exportButton]", this.gridElement).on("click", (e) => {
                e.preventDefault();

                let exportDialog = new ExportDialog({
                    exportURL: this.dataSource().lastGet.url,
                    exportColumns: this.columns.filter(c => c.field).map(c => c.field),
                    height: 210
                });

                exportDialog.init();
            });
            
        }
    }

    resize() {
        this.kendoObject.resize();
    }

    commandColumn() {
        const commands = this.commands.map((command) => ({
            ...command,
            template: command.template ? kendo.template(command.template) : null,
            href: command.href ? kendo.template(command.href) : null
        }));
        return (data) => {
            let result = "<div class='btn-group btn-group-sm'>";
            commands.forEach((command) => {
                if (command.template) {
                    result += command.template(data);
                } else if (command.href) {
                    const href = command.href(data);
                    result += "<a name='" + command.name + "' href='" + href + "'><span class='k-icon " + command.icon + "'></span>" + command.text + "</a>";
                } else {
                    result += '<button class="btn btn-primary" name="' + command.name + '"><span class="k-icon ' + command.icon + '"></span> ' + command.text + '</button>';
                }
            });
            return result + '</div>';
        };
    }

    dataBound(e) {
        let grid = this;
        let widget = $(this.element).data("widget");

        widget.commands.forEach(function (command) {
            if (command.click) {
                $("button[name=" + command.name + "]", widget.gridElement).on("click", function (e) {
                    e.preventDefault();
                    let item = grid.dataItem($(e.currentTarget).closest("tr"));
                    command.click(e, item, grid, widget);
                });
            }
        });
    }

    detailExpand(e) {
        let widget = $(this.element).data("widget");
        let item = this.dataItem(e.masterRow);

        let args = {
            event: e,
            item: item,
            grid: this,
            widget: widget,
            tabStrip: $("[name=tabs]", e.detailRow).kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            })
        };

        if (!item.$expanded) {
            widget.initilizeChildrenOf($(e.detailRow), args, function () {

                if (widget.onDetailInit) {
                    widget.onDetailInit(args);
                }
            });

            item.$expanded = true;
        }
    }
}
