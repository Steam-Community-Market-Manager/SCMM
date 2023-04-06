namespace SCMM.Worker.Server.Handlers
{
    /*
    [Concurrency(MaxConcurrentCalls = 1)]
    public class ImportProfileFriendsMessageHandler : IMessageHandler<ImportProfileFriendsMessage>
    {
        private readonly ICommandProcessor _commandProcessor;

        public ImportProfileFriendsMessageHandler(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public async Task HandleAsync(ImportProfileFriendsMessage message, IMessageContext context)
        {
            await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileFriendsRequest()
            {
                ProfileId = message.ProfileId,
                ImportInventoryAsync = true
            });
        }
    }
    */
}
