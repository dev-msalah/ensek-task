import React, { useState } from 'react';
import { Button, Form, Alert, Spinner, Table } from 'react-bootstrap';
import axios from 'axios';
import config from '../services/config';

interface UploadResult {
  successfulReadings: number;
  failedReadings: number;
  failures: Array<{
    reading:{
        accountId: number;
        meterReadingDateTime: Date;
        meterReadValue: number;
    }; 
    reason: string;
  }>;
}

const FileUpload: React.FC = () => {
  const [file, setFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [result, setResult] = useState<UploadResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [apiVersion, setApiVersion] = useState<string>('1.0');
  const [showCsvFormat, setShowCsvFormat] = useState(false);
  const [fileInputKey, setFileInputKey] = useState(0);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      setFile(e.target.files[0]);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) return;

    setFile(null);
    setResult(null);
    setIsUploading(true);
    setError(null);
    
    const MAX_FILE_SIZE = config.maxFileSizeInMB  * 1024 * 1024; // 5 MB
  if (file.size > MAX_FILE_SIZE) {
    setError("File size must be under 5MB.");
    setIsUploading(false);
    return;
  }

    try {
      const formData = new FormData();
      formData.append('meterReadingFile', file);

      const response = await axios.post<UploadResult>(
       `${config.apiBaseUrl}/api/v${apiVersion}/meter-reading-uploads`,
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        }
      );

      setResult(response.data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An unknown error occurred');
    } finally {
      setIsUploading(false);
      setFileInputKey(prev => prev + 1);
    }
  };

  
  return (
    <div className="container mt-5">
      <h2>ENSEK Meter Reading Upload</h2>
      
      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="apiVersion" className="mb-3">
          <Form.Label>API Version</Form.Label>
          <Form.Select 
            value={apiVersion} 
            onChange={(e) => setApiVersion(e.target.value)}
          >
            <option value="1.0">v1.0</option>
            <option value="2.0">v2.0</option>
          </Form.Select>
        </Form.Group>
        
        <Form.Group controlId="formFile" className="mb-3">
          <Form.Label>Choose CSV file</Form.Label>
          <Form.Control   key={fileInputKey}  type="file"  accept=".csv"  onChange={handleFileChange}/>
          <Form.Text className="text-muted">
            <Form.Label>Max file size {config.maxFileSizeInMB}MB</Form.Label><br/>
    <button
      type="button"
      onClick={() => setShowCsvFormat(!showCsvFormat)}
      className="btn btn-link p-0"
    >
      Show CSV format example
    </button>
    {showCsvFormat && (
      <div className="mt-2">
        <pre style={{ fontSize: '0.9rem' }}>
{`AccountId,MeterReadingDateTime,MeterReadValue
123,2024-04-05 09:00:00,12345
456,2024-04-06 10:00:00,67890`}
        </pre>
      </div>
    )}
  </Form.Text>
        </Form.Group>
        
        <Button variant="primary" type="submit" disabled={!file || isUploading}>
          {isUploading ? (
            <>
              <Spinner as="span" animation="border" size="sm" role="status" aria-hidden="true" />
              Uploading...
            </>
          ) : (
            'Upload'
          )}
        </Button>
      </Form>

      {error && (
        <Alert variant="danger" className="mt-3">
          {error}
        </Alert>
      )}

      {result && (
        <div className="mt-4">
          <h4>Upload Results</h4>
          <Alert variant={result.failedReadings > 0 ? 'warning' : 'success'}>
            Successful: {result.successfulReadings} | Failed: {result.failedReadings}
          </Alert>
          
          {result.failures.length > 0 && (
            <>
              <h5>Failure Details</h5>
              <Table striped bordered hover>
                <thead>
                  <tr>
                    <th>Account ID</th>
                    <th>Meter Reading Value</th>
                    <th>Meter Reading Submission Date</th>
                    <th>Reason</th>
                  </tr>
                </thead>
                <tbody>
                  {result.failures.map((failure, index) => (
                    <tr key={index}>
                      <td>{failure.reading.accountId}</td>
                      <td>{failure.reading.meterReadValue}</td>
                       <td>
                         {new Date(failure.reading.meterReadingDateTime).toLocaleDateString('en-GB', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit',
                            second: '2-digit'
                        })}
                       </td>
                      <td>{failure.reason}</td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default FileUpload;