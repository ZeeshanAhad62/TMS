window._dashboardCharts = window._dashboardCharts || {};

window._chartPalette = ['#3a6df0', '#22c3a6', '#f0a13a', '#e0554e', '#8b5cf6', '#0ea5e9', '#64748b', '#16a34a'];

window.renderDoughnutChart = (canvasId, labels, values) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    if (window._dashboardCharts[canvasId]) {
        window._dashboardCharts[canvasId].destroy();
    }

    window._dashboardCharts[canvasId] = new Chart(canvas, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: values,
                backgroundColor: labels.map((_, i) => window._chartPalette[i % window._chartPalette.length]),
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '62%',
            plugins: {
                legend: { position: 'right', labels: { boxWidth: 12, padding: 10 } }
            }
        }
    });
};

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
