window.dashboardCharts = {
    charts: {},

    renderTemperatureTrend: function (canvasId, labels, series) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        this.destroy(canvasId);

        this.charts[canvasId] = new Chart(canvas, {
            type: 'line',
            data: {
                labels: labels,
                datasets: series.map(s => ({
                    label: s.label,
                    data: s.data,
                    borderColor: s.color,
                    backgroundColor: this.hexToRgba(s.color, 0.12),
                    fill: true,
                    tension: 0.35,
                    pointRadius: 2,
                    borderWidth: 2
                }))
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: {
                        grid: { color: 'rgba(148, 163, 184, 0.12)' },
                        ticks: { color: '#8b98ac', callback: (v) => v + '°C' }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { color: '#8b98ac' }
                    }
                }
            }
        });
    },

    renderFloorEnergy: function (canvasId, labels, data) {
        // unchanged from before - already generic enough to reuse for sub-zones
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        this.destroy(canvasId);

        this.charts[canvasId] = new Chart(canvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: '#35d0b8',
                    borderRadius: 4,
                    barThickness: 18
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: {
                        grid: { color: 'rgba(148, 163, 184, 0.12)' },
                        ticks: { color: '#8b98ac', callback: (v) => v + ' kWh' }
                    },
                    y: {
                        grid: { display: false },
                        ticks: { color: '#8b98ac' }
                    }
                }
            }
        });
    },

    destroy: function (canvasId) {
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
            delete this.charts[canvasId];
        }
    },

    hexToRgba: function (hex, alpha) {
        const value = parseInt(hex.replace('#', ''), 16);
        const r = (value >> 16) & 255;
        const g = (value >> 8) & 255;
        const b = value & 255;
        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    }
};