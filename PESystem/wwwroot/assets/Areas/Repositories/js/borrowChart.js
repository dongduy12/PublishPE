// Chart hiển thị số lượng mượn/trả và aging

document.addEventListener("DOMContentLoaded", async function () {
    const borrowReturnEl = document.getElementById("borrow-return-chart");
    const borrowAgingEl = document.getElementById("borrow-aging-chart");

    if (borrowReturnEl) {
        try {
            const res = await fetch("http://10.220.130.119:9090/api/product/borrowed/daily");
            const json = await res.json();
            if (json.success) {
                const chart = echarts.init(borrowReturnEl);
                chart.setOption({
                    tooltip: { trigger: 'axis' },
                    xAxis: { type: 'category', data: ['Borrowed', 'Returned'] },
                    yAxis: { type: 'value' },
                    series: [{
                        type: 'bar',
                        data: [json.borrowedToday, json.returnedToday],
                        itemStyle: { color: '#2eca6a' }
                    }]
                });
                window.addEventListener('resize', () => chart.resize());
            }
        } catch (err) {
            console.error('Borrow/Return chart error', err);
        }
    }

    if (borrowAgingEl) {
        try {
            const res = await fetch("http://10.220.130.119:9090/api/product/borrowed/aging");
            const json = await res.json();
            if (json.success) {
                const labels = json.aging.map(a => a.days);
                const values = json.aging.map(a => a.count);
                const chart = echarts.init(borrowAgingEl);
                chart.setOption({
                    tooltip: { trigger: 'axis' },
                    xAxis: { type: 'category', data: labels },
                    yAxis: { type: 'value' },
                    series: [{ type: 'line', data: values, smooth: true, color: '#ff771d' }]
                });
                window.addEventListener('resize', () => chart.resize());
            }
        } catch (err) {
            console.error('Aging chart error', err);
        }
    }
});
