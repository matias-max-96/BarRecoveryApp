namespace BarRecoveryApp.ApplicationF.Services.Auditing
{
    public static class AuditActionCodes
    {
        public const string LoginSuccess = "LOGIN_SUCCESS";
        public const string Logout = "LOGOUT";

        public const string BarCreated = "BAR_CREATED";
        public const string BarUpdated = "BAR_UPDATED";
        public const string BarImported = "BAR_IMPORTED";

        public const string RecoveryReportCreated = "RECOVERY_REPORT_CREATED";

        public const string QualityInspectionCreated = "QUALITY_INSPECTION_CREATED";
        public const string BarApprovedForShipment = "BAR_APPROVED_FOR_SHIPMENT";
        public const string BarRejected = "BAR_REJECTED";
        public const string BarDisposed = "BAR_DISPOSED";
        public const string BarMarkedRecoverable = "BAR_MARKED_RECOVERABLE";

        public const string ShipmentCreated = "SHIPMENT_CREATED";
        public const string BarShipped = "BAR_SHIPPED";

        public const string BarReturnCreated = "BAR_RETURN_CREATED";
        public const string BarReturned = "BAR_RETURNED";

        public const string ReportExported = "REPORT_EXPORTED";

        public const string UserCreated = "USER_CREATED";
        public const string UserUpdated = "USER_UPDATED";

        public const string CatalogCreated = "CATALOG_CREATED";
        public const string CatalogUpdated = "CATALOG_UPDATED";
        public const string CatalogDeleted = "CATALOG_DELETED";
    }
}