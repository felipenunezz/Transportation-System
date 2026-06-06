let myMap;
const busMarkers = new Map();

DG.then(function() {
    myMap = DG.map('map', {
        center: [55.76, 37.64],
        zoom: 12
    });
});

function updateBusMarker(busId, latitude, longitude, busNumber, speed) {
    if (!myMap) return;

    const latLng = [latitude, longitude];

    if (busMarkers.has(busId)) {
        busMarkers.get(busId).setLatLng(latLng);
        busMarkers.get(busId).unbindPopup();
        busMarkers.get(busId).bindPopup(`<b>${busNumber}</b><br>Speed: ${speed} km/h`);
    } else {
        const marker = DG.marker(latLng)
            .bindPopup(`<b>${busNumber}</b><br>Speed: ${speed} km/h`)
            .addTo(myMap);
        busMarkers.set(busId, marker);
    }
}

function addStopMarker(stopId, name, latitude, longitude) {
    if (!myMap) return;

    DG.marker([latitude, longitude], {
        icon: DG.divIcon({
            className: 'stop-marker',
            html: '🚏',
            iconSize: [30, 30]
        })
    }).bindPopup(`<b>${name}</b>`).addTo(myMap);
}