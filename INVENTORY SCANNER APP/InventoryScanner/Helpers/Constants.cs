namespace InventoryScanner.Helpers
{
    /// <summary>
    /// Application-wide constants.
    /// </summary>
    public static class Constants
    {
        public const string DatabaseName = "inventoryscanner.db3";
        public const string AppName = "Inventory Scanner";
        public const string AppVersion = "1.0.0";

        // Default admin credentials (for first-launch seeding)
        public const string DefaultAdminUsername = "admin";
        public const string DefaultAdminPassword = "Admin@123";
        public const string DefaultAdminDisplayName = "Administrator";

        // SecureStorage keys
        public const string SessionTokenKey = "session_token";
        public const string CurrentUserKey = "current_user";
        public const string IsAuthenticatedKey = "is_authenticated";

        // Export
        public const string ExportFolderName = "InventoryExports";
        public const string ImportFolderName = "InventoryImports";

        // Date formats
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const string DateFormat = "yyyy-MM-dd";
    }
}
