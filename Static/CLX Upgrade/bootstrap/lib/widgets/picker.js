class Picker extends Dialog {
	constructor(args) {
		super(args);
		args = args || {};
		this.multiSelect = args.multiSelect;
		this.valueTemplate = args.valueTemplate;
		this.displayTemplate = args.displayTemplate;
		this.dataSource = args.dataSource || null;
		this.confirm = this.args.confirm || type.getResource("Core", "Confirm", session.culture).DisplayName;
		this.close = this.args.close || type.getResource("Core", "Close", session.culture).DisplayName;
		this.type = args.type;
		this.query = args.query;

		this.template = `
			<div class='dialog'>
				<input name='term' type='text' />
				<ul class='fieldList' name='items' style='overflow:scroll; overflow-x:hidden;height:400px;'>
				</ul>
				<div class='value'>
					<button name='confirm'>` + this.confirm + `</button>
					<button name='close'>` + this.close + `</button>
				</div>
			</div>
		`;
	}

	/*
			var p = new Picker({
				type: 'B2B/Company',
				valueTemplate: "#:Id#",
				multiSelect: false,
				displayTemplate: "<div class='item'>#:Name#</div>",
				query: "B2B/Company?$expand=References&$filter=References/any(r:contains(r/Id,'TERM')) or contains(Name,'TERM')&$top=50"
			});
			p.pick = function(result) {
				var CompanyId = result;
				var roleRow = $(e.target).closest("tr").prev();
				var role = $(roleRow).closest(".k-grid").data("kendoGrid").dataItem(roleRow);
				api.get("B2B/Company(" + result + ")", function(data) {
					if(data != null) {
						var companyData = data;
						grid.data("kendoGrid").dataSource.add(companyData);
					}
				});
			};
			p.init();
	/*/

	async search(term) {
		let queryReplaced = this.query.replaceAll("TERM", term);
		let data = await api.get(queryReplaced);
		this.dataSource.data(data.value);
		//this.dataSource.read();
		//this.list.data("kendoListView").refresh();
	}

	init(callback) {
		super.init(() => {
            const valueTemplate = kendo.template(this.valueTemplate);
            const displayTemplate = kendo.template(this.displayTemplate);
            const itemTemplate = (data) => "<li><input type='" + (this.multiSelect ? "checkbox" : "radio") + "' name='selected' value='" + valueTemplate(data) + "'></input>" + displayTemplate(data) + "</li>";

			let that = this;
			
			let build = () => {
				this.list = $("[name=items]", this.element).kendoListView({
					dataSource: this.dataSource,
					scrollable: true,
					template: kendo.template(itemTemplate)
				});

				$("[name=term]", this.element).on('keyup', function (e) {
					that.search($(this).val());
				});

				$("button[name='confirm']").on("click", () => {
					if(!this.multiSelect) {
						that.pick($("input[name='selected']:checked", that.element).val());
					} else {
						var rows = $("input[name='selected']:checked");
						var items = [];
						$.each(rows, function(i, v) { items.push($(this).val()); });
						this.pick(items);
					}
				});
			};
			if(!this.dataSource) {
				model.getDatasource({ endpoint: this.type }, (ds) => {
					this.dataSource = ds;
					build();
				});
			} else {
				build();
			}
			
		});
    }
}