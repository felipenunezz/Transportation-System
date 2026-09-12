using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Transportation_System.Data;
using Transportation_System.Hubs;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;

namespace Transportation_System.Services;

public class TelemetryProcessor(
    BusService busService,
    StopService stopService,
    IHubContext<BusTrackingHub> hub,
    ILogger<TelemetryProcessor> logger)
{
    public async Task ProcessBusAsync(int busId, string messageType, object dto)
    {
        switch (messageType, dto)   
        {
            case ("gps", Gps gps):
                await ProcessGpsAsync(busId, gps);
                break;
                
            case ("speed", Speedometer speed):
                await ProcessSpeedAsync(busId, speed);
                break;
                
            case ("passengers", BusPassenger passenger):
                await ProcessPassengerAsync(busId, passenger);
                break;
                
            case ("terminal" , Terminal terminal):
                await ProcessTerminalAsync(busId, terminal);
                break;
                
            default:
                logger.LogWarning("Unknown bus telemetry type: {MessageType} for bus {BusId}", messageType, busId);
                break;
        }
    }

    //Stop processing
    public async Task ProcessStopAsync(int stopId, StopDto passenger)
    {
        if (passenger.WaitingPassengers < 0)
        {
            logger.LogError("Occupancy for stop {StopId} is not valid: {@Occupancy}", stopId, passenger);
            return;
        }

        try
        {
            await stopService.UpdateStopAsync(stopId, passenger);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving occupancy for stop {StopId}", stopId);
            return;
        }

        await hub.Clients.All.SendAsync("StopUpdated", stopId);
    }

    //Bus processing
    private async Task ProcessGpsAsync(int busId, Gps gps)
    {
        bool ValidCoordinates =  true;
        
        if (gps is { CurrentLatitude: 0, CurrentLongitude: 0 }) ValidCoordinates = false;
        if (gps.CurrentLatitude  is < -90  or > 90) ValidCoordinates = false;
        if (gps.CurrentLongitude is < -180 or > 180) ValidCoordinates = false;
        
        if  (!ValidCoordinates)
        {
            logger.LogError("Invalid latitude:{Latitude} or longitude: {Longitude}", gps.CurrentLatitude, gps.CurrentLongitude);
        }

        try
        {
            await busService.UpdateBusGpsAsync(busId, gps);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving gps: {Gps}", gps);
            return;
        }
        await hub.Clients.All.SendAsync("GpsUpdated", gps);
    }

    private async Task ProcessSpeedAsync(int busId, Speedometer speed)
    {
        if (speed.CurrentSpeed < 0 || speed.CurrentSpeed > 80)
        {
            logger.LogError("Invalid speed: {Speed}", speed);
            return;
        }

        try
        {
            await busService.UpdateBusSpeedAsync(busId, speed);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving gps: {Speed}", speed);
            return;
        }
        await hub.Clients.All.SendAsync("SpeedUpdated", speed);
    }

    private async Task ProcessPassengerAsync(int busId, BusPassenger passenger)
    {
        if (passenger.PassengerCount < 0)
        {
            logger.LogError("Invalid speed: {Speed}", passenger);
            return;
        }

        try
        {
            await busService.UpdateBusPassengerAsync(busId, passenger);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving gps: {Speed}", passenger);
            return;
        }
        await hub.Clients.All.SendAsync("SpeedUpdated", passenger);  
    }

    private async Task ProcessTerminalAsync(int busId, Terminal terminal)
    {
        bool ValidTerminal = true;
        
        if (terminal.BusStatus == BusStatus.Moving && terminal.OnRoute) ValidTerminal = false;
        if (terminal.RouteId <= 0) ValidTerminal = false;
        if (terminal.CurrentStopId == new StopQueue(terminal.StopQueue).Peek()) ValidTerminal = false;
        if (terminal.CurrentStopId == null && terminal.BusStatus == BusStatus.AtStop) ValidTerminal = false;
        if (terminal is { OnRoute: true, StopQueue.Count: 0 }) ValidTerminal = false;
        
        if (!ValidTerminal) logger.LogError("Invalid terminal: {Terminal}", terminal);

        try
        {
            await busService.UpdateBusTerminalAsync(busId, terminal);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving terminal: {Terminal}", terminal);
            return;
        }
        await hub.Clients.All.SendAsync("TerminalUpdated", terminal);
    }
}
