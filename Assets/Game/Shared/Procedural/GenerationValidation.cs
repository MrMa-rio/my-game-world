namespace MyGameWorld.Shared.Procedural
{
    public readonly struct GenerationValidation
    {
        private GenerationValidation(bool isValid, string errorCode, string message)
        {
            IsValid = isValid;
            ErrorCode = errorCode;
            Message = message;
        }

        public bool IsValid { get; }

        public string ErrorCode { get; }

        public string Message { get; }

        public static GenerationValidation Valid() => new GenerationValidation(true, string.Empty, string.Empty);

        public static GenerationValidation Invalid(string errorCode, string message)
        {
            return new GenerationValidation(false, errorCode, message);
        }
    }
}
