window.dashboardCharts = {
    charts: {},

    renderTemperatureTrend: function (canvasId, temperatureSeries) {
        this.waitForElement(canvasId, (canvas) => {

            if (!temperatureSeries || temperatureSeries.length === 0) {
                return;
            }

            this.destroy(canvasId);

            const longest = temperatureSeries.reduce(
                (a, b) => (a.points.length > b.points.length ? a : b), { points: [] });
            const labels = longest.points.map(point => {
                const date = new Date(point.timestamp);
                return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
            });

            this.charts[canvasId] = new Chart(canvas, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: temperatureSeries.map(series => ({
                        label: series.label,
                        data: series.points.map(point => point.value),
                        borderColor: series.color,
                        backgroundColor: this.hexToRgba(series.color, 0.12),
                        fill: true,
                        tension: 0.35,
                        pointRadius: 2,
                        borderWidth: 2
                    }))
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    animation: {
                        duration: 700,
                        easing: 'easeOutCubic'
                    },
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
        });
    },

    renderFloorEnergy: function (canvasId, labels, data) {
        this.waitForElement(canvasId, (canvas) => {

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
                    animation: {
                        duration: 700,
                        easing: 'easeOutCubic'
                    },
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
        });
    },

    waitForElement: function (id, callback, attemptsLeft = 20) {
        const el = document.getElementById(id);
        if (el) {
            callback(el);
            return;
        }
        if (attemptsLeft <= 0) {
            console.warn(`dashboardCharts: canvas #${id} never appeared in the DOM.`);
            return;
        }
        setTimeout(() => this.waitForElement(id, callback, attemptsLeft - 1), 50);
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