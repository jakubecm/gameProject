namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The captions of the desktop's own controls (buttons, hints), all in one place. The texts of the game itself
    /// (knowledge base, web pages, AI messages) are data files in Assets/_Project/Data.
    /// </summary>
    public static class DesktopTexts
    {
        // The computer in the room
        public const string UsePrompt = "Use computer";
        public const string TurnOnPrompt = "Turn on computer";
        public const string LeaveHint = "Esc - step away from the computer";

        // The desktop
        public const string Start = "Start";
        public const string Close = "X";
        public const string ShutDown = "Shut down";
        public const string PowerOnHint = "Click to power on";
        public const string BootTitle = "Service Terminal OS";
        public const string BootStarting = "Starting services...";
        public const string BootShuttingDown = "Shutting down...";

        // Knowledge base
        public const string KnowledgeTitle = "Knowledge Base";
        public const string KnowledgeChoose = "Select a document on the left.";
        public const string KnowledgeEmpty = "No documents.";
        public const string ReadingPauseNote = "The game is paused while you read.";

        // Browser
        public const string BrowserTitle = "Browser";
        public const string BrowserBack = "Back";
        public const string BrowserHome = "Home";
        public const string BrowserNotFound = "Page not found.";

        // The start page
        public const string PortalFooter = "Internal network - authorised staff only";

        // The news site (AAC News)
        public const string NewsName = "AAC";
        public const string NewsWord = "NEWS";
        public const string NewsTagline = "News for the few people who still care what is happening";
        public const string NewsRelated = "Related stories";
        public const string NewsNoStories = "No stories here yet.";
        public const string NewsPhoto = "photo";

        // Password minigame
        public const string PasswordTitle = "Security";
        public const string PasswordHeading = "Access locked";
        public const string PasswordInstructions = "Pick the password from the list. Each attempt shows how many letters are in the right place.";
        public const string PasswordTries = "Attempts";
        public const string PasswordLog = "Attempt log";
        public const string PasswordMatch = "match";
        public const string PasswordWon = "ACCESS GRANTED";
        public const string PasswordLost = "LOCKED OUT";
        public const string PasswordWasLabel = "The password was";
        public const string PasswordRetry = "Try again";
        public const string PasswordNoWords = "The word list is missing.";
    }
}
