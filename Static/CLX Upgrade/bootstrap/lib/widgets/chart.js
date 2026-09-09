class Chart extends Widget
{
    constructor(element, args) {
        super(element, args);
        this.chartElement = $(element).append("<div></div>").children().first();
        this.text = args.text;
        this.showLegend = args.showLegend;
        this.series = args.series || [];
        this.categories = args.categories || [];
        this.max = args.max;
        this.type = args.type || "bar";
        this.showMinorLines = args.showMinorLines;
        this.valueTemplate = args.valueTemplate ||function (data) { return "" + (data.value) + ""; };
        this.categoryTemplate = args.categoryTemplate || function (data) { return "" + (data.value) + ""; };
        this.tooltipTemplate = args.tooltipTemplate || function (data) { return "" + (data.series.name) + ": " + (data.value) + ""; };;
        this.axisCrossingValue = args.axisCrossingValue || 0;
        this.colors = args.colors || session.app.Config.Themes.Default.colours.charts;

        for (let i in this.series) {
            this.series[i].color = this.colors[i];
        }
    }

    init() {
        this.kendoObject = $(this.chartElement).kendoChart({
            axisCrossingValue: this.axisCrossingValue,
            title: { text: this.text },
            legend: { visible: this.showLegend, position: "top" },
            seriesDefaults: { type: this.type },
            series: this.series,
            valueAxis: {
                max: this.max + ((this.max / 100) * 5),
                line: { visible: false },
                minorGridLines: { visible: this.showMinorLines },
                labels: { rotation: "auto", template: this.valueTemplate }
            },
            categoryAxis: {
                categories: this.categories,
                majorGridLines: { visible: false },
                labels: { rotation: "auto", template: this.categoryTemplate }
            },
            tooltip: {
                visible: true,
                template: this.tooltipTemplate
            }
        }).data('kendoChart');
    }
}