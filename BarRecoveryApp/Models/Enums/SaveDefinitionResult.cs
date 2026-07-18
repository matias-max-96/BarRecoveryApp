namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public enum SaveDefinitionResult
    {
        Success,
        NotAuthenticated,
        NoPermission,
        InvalidCode,
        InvalidName,
        InvalidRangeConfiguration,
        DuplicateCode,
        NotFound
    }
}