public class GetPrompt
{
    public string Prompt { get; private set; }
    
    public GetPrompt(string vendorData, string bzbsData)
    {
        Prompt = GeneratePrompt(vendorData, bzbsData);
    }

    public string GetPromptText(string vendorData, string bzbsData)
    {
        Prompt = GeneratePrompt(vendorData, bzbsData);
        return Prompt;
    }

    private string GeneratePrompt(string vendorData, string bzbsData)
    {
        return $@"
            Here is the invoice data that is read from vendor invoice:
            -- VENDOR DATA STARTS HERE --

            {vendorData}

            -- VENDOR DATA ENDS HERE --

            And, here is the product data from Buzzebees database:
            -- BUZZEBEES DATA STARTS HERE --

            {bzbsData}

            -- BUZZEBEES DATA ENDS HERE --

            Please provide the matching result in JSON format.
            Response only JSON output, no explanation is required.
        ";
    }
}