# MarkitDown Service

A Python Flask web service that converts various file formats to markdown using the [MarkitDown](https://github.com/microsoft/markitdown) library.

## Features

- Converts various file formats (PDF, Word documents, PowerPoint, Excel, images, etc.) to markdown
- RESTful API with file upload support
- URL-based conversion support
- Docker containerization
- Health check endpoint

## API Endpoints

### Health Check
```
GET /health
```
Returns service health status.

### File Upload Conversion
```
POST /convert
```
Converts an uploaded file to markdown.

**Request:**
- Method: POST
- Content-Type: multipart/form-data
- Body: File in 'file' field
- Optional: 'filename' parameter

**Response:**
```json
{
  "success": true,
  "filename": "document.pdf",
  "markdown": "# Document Title\n\nContent...",
  "original_size": 1024,
  "markdown_size": 512
}
```

### URL Conversion
```
POST /convert-url
```
Converts a file from a URL to markdown.

**Request:**
```json
{
  "url": "https://example.com/document.pdf"
}
```

**Response:**
```json
{
  "success": true,
  "url": "https://example.com/document.pdf",
  "markdown": "# Document Title\n\nContent...",
  "markdown_size": 512
}
```

## Running the Service

### Local Development
```bash
pip install -r requirements.txt
python app.py
```

### Docker
```bash
docker build -t markitdown-service .
docker run -p 5000:5000 markitdown-service
```

### Environment Variables
- `PORT`: Port to run the service on (default: 5000)
- `DEBUG`: Enable debug mode (default: False)

## Usage with .NET

The service is designed to be called from the TextExtractionHandler in the SemanticKernel.Agents.Memory.Core project.

Example HTTP client usage:
```csharp
using var httpClient = new HttpClient();
using var form = new MultipartFormDataContent();
using var fileContent = new ByteArrayContent(fileBytes);
fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
form.Add(fileContent, "file", fileName);

var response = await httpClient.PostAsync("http://markitdown-service:5000/convert", form);
var result = await response.Content.ReadAsStringAsync();
```
