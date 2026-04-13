namespace MarathonTraining.Application.Common.Exceptions;

public class StravaNotConnectedException()
    : Exception("No Strava connection found. Connect your Strava account before syncing activities.");
