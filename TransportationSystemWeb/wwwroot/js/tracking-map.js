// Leaflet interop for the GPS / Live Tracking module (module 9).
// One map instance per element id; layers are swapped wholesale on update.
window.trackingMap = (function () {
    const maps = {};

    const COLORS = { Moving: '#16a34a', Idle: '#f0a13a', Offline: '#94a3b8' };

    function get(id) { return maps[id]; }

    function ensure(id, center, zoom) {
        let m = maps[id];
        if (m) return m;
        const el = document.getElementById(id);
        if (!el || !window.L) return null;

        const map = L.map(id, { zoomControl: true }).setView(center || [20, 0], zoom || 2);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        m = maps[id] = {
            map,
            vehicleLayer: L.layerGroup().addTo(map),
            pathLayer: L.layerGroup().addTo(map),
            fenceLayer: L.layerGroup().addTo(map),
            pickMarker: null
        };
        // Leaflet mis-sizes when created in a hidden/animating container.
        setTimeout(() => map.invalidateSize(), 120);
        return m;
    }

    function dot(color) {
        return L.divIcon({
            className: 'tracking-dot',
            html: `<span style="display:block;width:14px;height:14px;border-radius:50%;background:${color};border:2px solid #fff;box-shadow:0 0 0 1px rgba(0,0,0,.3)"></span>`,
            iconSize: [14, 14],
            iconAnchor: [7, 7]
        });
    }

    return {
        init: function (id, center, zoom) {
            ensure(id, center, zoom);
        },

        setVehicles: function (id, vehicles) {
            const m = ensure(id);
            if (!m) return;
            m.vehicleLayer.clearLayers();
            const pts = [];
            (vehicles || []).forEach(v => {
                if (!v.hasPosition || v.latitude == null) return;
                const color = COLORS[v.movementState] || COLORS.Offline;
                const marker = L.marker([v.latitude, v.longitude], { icon: dot(color) });
                const age = v.minutesSinceReport == null ? 'n/a' : `${v.minutesSinceReport} min ago`;
                marker.bindPopup(
                    `<strong>${v.vehicleCode}</strong> &mdash; ${v.registrationNumber}<br>` +
                    `${v.movementState} &middot; ${v.speedKph == null ? '?' : v.speedKph} km/h<br>` +
                    `<span style="color:#666">${age}</span>`
                );
                marker.addTo(m.vehicleLayer);
                pts.push([v.latitude, v.longitude]);
            });
            if (pts.length) m.map.fitBounds(pts, { padding: [40, 40], maxZoom: 14 });
        },

        drawPath: function (id, points) {
            const m = ensure(id);
            if (!m) return;
            m.pathLayer.clearLayers();
            const latlngs = (points || []).map(p => [p.latitude, p.longitude]);
            if (!latlngs.length) return;
            L.polyline(latlngs, { color: '#3a6df0', weight: 4, opacity: 0.8 }).addTo(m.pathLayer);
            L.marker(latlngs[0], { icon: dot('#16a34a') }).bindPopup('Start').addTo(m.pathLayer);
            L.marker(latlngs[latlngs.length - 1], { icon: dot('#e0554e') }).bindPopup('End').addTo(m.pathLayer);
            m.map.fitBounds(latlngs, { padding: [40, 40], maxZoom: 15 });
        },

        setGeofences: function (id, fences) {
            const m = ensure(id);
            if (!m) return;
            m.fenceLayer.clearLayers();
            const bounds = [];
            (fences || []).forEach(f => {
                const style = { color: f.isActive ? '#8b5cf6' : '#94a3b8', weight: 2, fillOpacity: 0.08 };
                if (f.shape === 'Circle' && f.centerLat != null && f.radiusMeters) {
                    const c = L.circle([f.centerLat, f.centerLng], { ...style, radius: f.radiusMeters }).addTo(m.fenceLayer);
                    c.bindPopup(`<strong>${f.name}</strong>`);
                    bounds.push([f.centerLat, f.centerLng]);
                } else if (f.polygon && f.polygon.length >= 3) {
                    const latlngs = f.polygon.map(p => [p.lat, p.lng]);
                    const poly = L.polygon(latlngs, style).addTo(m.fenceLayer);
                    poly.bindPopup(`<strong>${f.name}</strong>`);
                    latlngs.forEach(ll => bounds.push(ll));
                }
            });
            if (bounds.length) m.map.fitBounds(bounds, { padding: [40, 40], maxZoom: 14 });
        },

        // Single preview circle for the geofence editor.
        showCircle: function (id, lat, lng, radius) {
            const m = ensure(id, [lat, lng], 12);
            if (!m) return;
            m.fenceLayer.clearLayers();
            if (lat == null || lng == null) return;
            L.circle([lat, lng], { color: '#8b5cf6', weight: 2, fillOpacity: 0.1, radius: radius || 200 }).addTo(m.fenceLayer);
            m.map.setView([lat, lng], m.map.getZoom() < 10 ? 13 : m.map.getZoom());
        },

        enablePicker: function (id, dotNetRef) {
            const m = ensure(id);
            if (!m) return;
            m.map.on('click', function (e) {
                dotNetRef.invokeMethodAsync('OnMapClick', e.latlng.lat, e.latlng.lng);
            });
        },

        invalidate: function (id) {
            const m = get(id);
            if (m) m.map.invalidateSize();
        },

        dispose: function (id) {
            const m = maps[id];
            if (!m) return;
            m.map.remove();
            delete maps[id];
        }
    };
})();
