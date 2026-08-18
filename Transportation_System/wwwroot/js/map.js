let map;
let selectionMap;
let selectMarker;

const busMarkers = new Map();
const stopMarkers = new Map();

// Raster source pointed at the public OSM tile server — same tiles as
// before, just served through MapLibre's style spec instead of Leaflet's
// tileLayer. Note: tile.openstreetmap.org's usage policy expects light,
// non-commercial traffic with a real User-Agent — fine for dev/small
// deployments, but for anything heavier consider a provider like MapTiler
// (free tier) or self-hosting tiles later.
const OSM_STYLE = {
    version: 8,
    sources: {
        'osm-raster': {
            type: 'raster',
            tiles: ['https://tile.openstreetmap.org/{z}/{x}/{y}.png'],
            tileSize: 256,
            attribution: '&copy; OpenStreetMap contributors'
        }
    },
    layers: [
        { id: 'osm-raster-layer', type: 'raster', source: 'osm-raster' }
    ]
};

function createIconElement(emoji, sizePx) {
    const el = document.createElement('div');
    el.style.fontSize = `${sizePx}px`;
    el.style.lineHeight = '1';
    el.textContent = emoji;
    return el;
}

function updateBusMarker(bus) {
    const lngLat = [bus.currentLongitude, bus.currentLatitude]; // MapLibre order: [lng, lat]
    const popupHtml = `<b>${bus.busNumber}</b><br>Speed: ${bus.speed} km/h<br>Passengers: ${bus.passengerCount}`;

    if (busMarkers.has(bus.id)) {
        const marker = busMarkers.get(bus.id);
        marker.setLngLat(lngLat);
        marker.getPopup().setHTML(popupHtml);
    } else {
        const popup = new maplibregl.Popup({ offset: 15 }).setHTML(popupHtml);
        const marker = new maplibregl.Marker({ element: createIconElement('🚌', 26) })
            .setLngLat(lngLat)
            .setPopup(popup)
            .addTo(map);
        busMarkers.set(bus.id, marker);
    }
}

function addStopMarker(stopId, name, latitude, longitude, waitingPassengers) {
    const popup = new maplibregl.Popup({ offset: 12 })
        .setHTML(`<b>${name}</b><br>Waiting: ${waitingPassengers} passengers`);
    const marker = new maplibregl.Marker({ element: createIconElement('🚏', 22) })
        .setLngLat([longitude, latitude])
        .setPopup(popup)
        .addTo(map);
    stopMarkers.set(stopId, marker);
}

let routeLayerCount = 0;

function drawRoute(routeCoordinates, color = '#3388ff') {
    // Called with [[lat, lng], ...] (matches the shape loadMapData already
    // builds) — MapLibre/GeoJSON wants [lng, lat].
    const coords = routeCoordinates.map(([lat, lng]) => [lng, lat]);
    const id = `route-${routeLayerCount++}`;

    map.addSource(id, {
        type: 'geojson',
        data: { type: 'Feature', geometry: { type: 'LineString', coordinates: coords } }
    });

    map.addLayer({
        id,
        type: 'line',
        source: id,
        paint: {
            'line-color': color,
            'line-width': 3,
            'line-opacity': 0.7
        }
    });
}

function clearRoutes() {
    for (let i = 0; i < routeLayerCount; i++) {
        const id = `route-${i}`;
        if (map.getLayer(id)) map.removeLayer(id);
        if (map.getSource(id)) map.removeSource(id);
    }
    routeLayerCount = 0;
}

async function loadMapData() {
    try {
        const response = await fetch('/api/mapdata');
        const data = await response.json();

        console.log('Loaded data:', data);

        clearRoutes();

        if (data.stops && data.stops.length > 0) {
            data.stops.forEach(stop => {
                addStopMarker(stop.id, stop.name, stop.latitude, stop.longitude, stop.waitingPassengers);
            });
            console.log(`Added ${data.stops.length} stops`);
        }

        if (data.routes && data.routes.length > 0) {
            const colors = ['#3388ff', '#ff6633', '#33cc33', '#ff33cc'];
            data.routes.forEach((route, index) => {
                if (route.stops && route.stops.length > 1) {
                    const coordinates = route.stops.map(p => [p.latitude, p.longitude]);
                    drawRoute(coordinates, colors[index % colors.length]);
                }
            });
            console.log(`Added ${data.routes.length} routes`);
        }

        if (data.buses && data.buses.length > 0) {
            data.buses.forEach(bus => {
                if (bus.currentLatitude && bus.currentLongitude) {
                    updateBusMarker(bus);
                }
            });
            console.log(`Added ${data.buses.length} buses`);
        }

        const allLngLats = [
            ...[...busMarkers.values()].map(m => m.getLngLat()),
            ...[...stopMarkers.values()].map(m => m.getLngLat())
        ];
        if (allLngLats.length > 0) {
            const bounds = allLngLats.reduce(
                (b, ll) => b.extend(ll),
                new maplibregl.LngLatBounds(allLngLats[0], allLngLats[0])
            );
            map.fitBounds(bounds, { padding: 40 });
        }
    } catch (error) {
        console.error('Error loading map data:', error);
    }
}

function updateBusRealtime(bus) {
    updateBusMarker(bus);
}

document.addEventListener('DOMContentLoaded', function () {
    if (!document.getElementById('map')) return; // this page has no dashboard map

    map = new maplibregl.Map({
        container: 'map',
        style: OSM_STYLE,
        center: [44.006516, 56.326797], // MapLibre: [lng, lat] — note the flip from Leaflet's [lat, lng]
        zoom: 13
    });

    map.on('load', loadMapData); // addSource/addLayer (used by drawRoute) require the style to be loaded first
});

window.updateBusMarker = updateBusMarker;
window.addStopMarker = addStopMarker;
window.drawRoute = drawRoute;
window.loadMapData = loadMapData;
window.updateBusRealtime = updateBusRealtime;

document.addEventListener('shown.bs.modal', function (e) {
    const modal = e.target;
    const mapContainer = modal.querySelector('#selectionMap');
    if (!mapContainer) return;

    const latInput = modal.querySelector('#Latitude');
    const lngInput = modal.querySelector('#Longitude');
    const displayLat = modal.querySelector('#displayLat');
    const displayLng = modal.querySelector('#displayLng');
    
    //for _Details.cshtml and _Delete.cshtml
    const enableSelection = mapContainer.dataset.enableSelection === 'true';

    if (selectionMap) {
        selectionMap.remove();
        selectionMap = null;
        selectMarker = null;
    }

    selectionMap = new maplibregl.Map({
        container: mapContainer,
        style: OSM_STYLE,
        center: [44.006516, 56.326797],
        zoom: 13
    });

    const lat = parseFloat(latInput?.value);
    const lng = parseFloat(lngInput?.value);
    const hasExistingLocation = !isNaN(lat) && !isNaN(lng) && !(lat === 0 && lng === 0);

    selectionMap.on('load', () => {
        if (hasExistingLocation) {
            selectMarker = new maplibregl.Marker().setLngLat([lng, lat]).addTo(selectionMap);
            selectionMap.setCenter([lng, lat]);
            selectionMap.setZoom(15);
        }
        selectionMap.resize();
    });

    if (enableSelection) {
        selectionMap.on('click', function (e) {
        const lat = e.latlng.lat.toFixed(6);
        const lng = e.latlng.lng.toFixed(6);

        latInput.value = lat;
        lngInput.value = lng;
        if (displayLat) displayLat.textContent = lat;
        if (displayLng) displayLng.textContent = lng;

        if (selectMarker) selectMarker.remove();
        selectMarker = new maplibregl.Marker().setLngLat(e.lngLat).addTo(selectionMap);
    });
    }
    else {
        displayLat.textContent = lat;
        displayLng.textContent = lng;
    }
    setTimeout(() => selectionMap.resize(), 100);
});