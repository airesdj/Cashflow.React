namespace CashFlow.Core.Classes
{
    public enum LocationItem
    {
        VIEW_ALL_LOCATIONS = -1
    }
    public enum ImageType
    {
        Front = 1,
        Back = 2,
        WebCam = 3
    }
    public enum UserRole
    {
        Super = 1,
        Admin = 2,
        Staff = 3,
        ReadOnly = 4
    }

    public enum MenuItem
    {
        CurrentVisit = 1,
        VisitLog = 2,
        Visitors = 3,
        Contractor = 4,
        Staff = 5,
        Couriers = 6,
        Volunteers = 7,
        Group = 8,
        Training = 9,
        Notification = 10,
        AutomaticSignOut = 11,
        Reports = 12,
        Users = 13,
        Drivers = 14
    }

    public enum PurgeType
    {
        Ban = 1,
        MemberJoin = 2,
        Visitor = 3
    }


    public enum ReportType
    {
        VisitorReport = 1,
        BanExclusionReport = 2,
        VisitorSummaryReport = 3,
        MemberJoinReport = 4
    }

    public enum ReportFilterType
    {
        ReportDateRange = 1,
        AgeRange = 2,
        VisitorSelection = 3,
        AlertType = 4,
        BanType = 5,
        BanDuration = 6,
        TimeFilter = 7,
        Terminal = 8,
        Postcode = 9,
        RepeatVisitor = 10,
        DataCollection = 11
    }

    public enum ReportIntervalType
    {
        Once = 1,
        Day = 2,
        Week = 3,
        Month = 4
    }


    public enum ReportDateRange
    {
        Today = 1,
        ThisWeek = 2,
        ThisMonth = 3,
        PreviousWeek = 4,
        PreviousMonth = 5
    }
}
