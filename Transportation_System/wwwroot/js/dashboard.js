
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/busHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();


connection.start().then(() => {
    console.log("Connected to SignalR hub");
    connection.invoke("RequestBusUpdates");
}).catch(err => console.error(err));


connection.on("BusLocationUpdated", (data) => {
    console.log("Bus update received:", data);
    
    updateBusMarker(
        data.busId,
        data.latitude,
        data.longitude,
        `BUS-${data.busId.toString().padStart(3, '0')}`,
        data.speed
    );
    
    const busCard = document.querySelector(`.bus-card[data-bus-id="${data.busId}"]`);
    if (busCard) {
        const speedElement = busCard.querySelector('.speed');
        if (speedElement) {
            speedElement.textContent = data.speed;
        }
    }
});

document.addEventListener('DOMContentLoaded', () => {
   
    document.addEventListener('DOMContentLoaded', () => {
        const stops = window.__dashboardData?.busStops || [];
        stops.forEach(stop => {
            addStopMarker(stop.id, stop.name, stop.latitude, stop.longitude);
        });
    });
    
    stops.forEach(stop => {
        addStopMarker(stop.id, stop.name, stop.latitude, stop.longitude);
    });
});