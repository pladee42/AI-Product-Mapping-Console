
# Product Data Processing Tool

This project is a console application designed to process product data by interacting with Azure OpenAI and BZBS (Buzzebees) systems. The application reads data in JSON format, matches product details, and exports the results to an Excel file with conditional formatting based on probability values.

## Features

- Interacts with Azure OpenAI for data extraction and matching.
- Integrates with BZBS to fetch and process product data.
- Exports results to an Excel file (.xlsx) with highlighted cells based on specific criteria.
- Configurable through JSON settings files.

## Files Overview

- **Program.cs**: Main entry point of the application. It handles configuration setup, initiates the data extraction and matching process, and calls the function to save results in an Excel format.
  
- **AzureOpenAIClient.cs**: Handles communication with Azure OpenAI, including managing threads and retrieving chat history for data processing.
  
- **BZBSClient.cs**: Manages interaction with BZBS endpoints to add and retrieve files, supporting the matching process by supplying necessary data.
  
- **GetPrompt.cs**: Generates prompts for interacting with Azure OpenAI based on vendor and BZBS data, facilitating the extraction of matched results.

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/pladee42/AI-Product-Mapping-Console.git
   ```
   
2. Navigate to the project directory:
   ```bash
   cd product-data-processing-tool
   ```

3. Install dependencies using NuGet:
   ```bash
   dotnet restore
   ```

4. Ensure the following NuGet packages are installed:
   - `EPPlus` for Excel file creation and manipulation.
   - `Newtonsoft.Json` for JSON parsing and manipulation.

## Configuration

The application uses an `appsettings.json` file for configuration. Ensure your configuration file is structured as follows:

```json
{
    "AzureOpenAI": {
        "Endpoint": "your-azure-openai-endpoint",
        "ApiKey": "your-azure-openai-api-key",
        "ApiVersion": "your-api-version",
        "Model": "your-model"
    },
    "BZBS": {
        "Endpoint": "your-bzbs-endpoint",
        "BlobUrl": "your-bzbs-blob-url"
    },
    "ProductExtractorAgentId": "your-extractor-agent-id",
    "ProductMatchingAgentId": "your-matching-agent-id"
}
```

### Key Settings:
- **AzureOpenAI**: Includes the endpoint, API key, API version, and model details for connecting to Azure OpenAI services.
- **BZBS**: Contains the endpoint and blob URL configuration for interacting with BZBS data services.
- **Agent IDs**: The `ProductExtractorAgentId` and `ProductMatchingAgentId` are used to identify the specific agents used for data processing.

## Usage

1. Run the application:
   ```bash
   dotnet run
   ```

2. Follow the prompts to input the vendor and BZBS file names.

3. The application will process the data and export the results to an Excel file in the `output` folder.

## Excel Export

The `SaveToExcel` function saves the processed data to an Excel file with conditional formatting:

- Cells in the **Probability** column are highlighted based on these criteria:
  - **Red**: Probability lower than 0.21.
  - **Yellow**: Probability between 0.21 and 0.41.

## Error Handling

The application includes basic error handling to manage issues during data parsing and API interactions. Ensure all required API keys and configuration settings are correctly set to avoid runtime errors.

## Contributing

1. Fork the repository.
2. Create a new branch (`git checkout -b feature-branch`).
3. Commit your changes (`git commit -m 'Add new feature'`).
4. Push to the branch (`git push origin feature-branch`).
5. Open a Pull Request.
