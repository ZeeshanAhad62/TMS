window._dashboardCharts = window._dashboardCharts || {};

window.renderBarChart = (canvasId, labels, values, color) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    if (window._dashboardCharts[canvasId]) {
        window._dashboardCharts[canvasId].destroy();
    }

    window._dashboardCharts[canvasId] = new Chart(canvas, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                data: values,
                backgroundColor: color,
                borderRadius: 6,
                maxBarThickness: 40
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false }
            },
            scales: {
                y: { beginAtZero: true, ticks: { precision: 0 } }
            }
        }
    });
};
