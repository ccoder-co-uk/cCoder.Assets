class PieChart extends Widget {
    constructor(element, data) {
        super(element);
        this.data = data;
        this.chartName = $(element).attr("name");
        $(element).append("<div name='" + this.gridName + "PieChart'></div>");
        this.chartElement = $("[name=" + this.gridName + "PieChart]", $(element));
    }
    
    init() {
        this.kendoObject = $(this.chartElement).kendoChart({
            seriesDefaults: {
                labels: {
                    visible: true,
                    background: "transparent",
                    template: function (data) { return "" + (data.category) + ": " + (kendo.toString(data.value, type.aggregateMoneyFormat)) + ""; },
                }
            },
            legend: {
                align: "right"
            },
            series: [{
                type: "pie",
                startAngle: 150,
                data: this.data,
                padding: 60
            }]
        }).data("kendoChart");
    }
    
    setData(data) {
        this.kendoObject.options.series[0].data = data;
    }
    
    refresh() {
        this.kendoObject.refresh();
    }
}
