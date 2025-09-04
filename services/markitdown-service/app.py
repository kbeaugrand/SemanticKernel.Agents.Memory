#!/usr/bin/env python3
"""
MarkitDown Flask Service

A web service that converts various file formats to markdown using the MarkitDown library.
Accepts file uploads via HTTP and returns the markdown representation.
"""

import os
import tempfile
import traceback
from flask import Flask, request, jsonify
from markitdown import MarkItDown
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = Flask(__name__)

# Initialize MarkItDown converter
markitdown = MarkItDown()

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({'status': 'healthy', 'service': 'markitdown-service'})

@app.route('/convert', methods=['POST'])
def convert_file():
    """
    Convert uploaded file to markdown
    
    Expects:
    - File upload in 'file' field
    - Optional filename parameter
    
    Returns:
    - JSON with markdown content or error message
    """
    try:
        # Check if file is present in request
        if 'file' not in request.files:
            return jsonify({'error': 'No file provided'}), 400
        
        file = request.files['file']
        
        if file.filename == '':
            return jsonify({'error': 'No file selected'}), 400
        
        # Get original filename or use provided filename
        filename = request.form.get('filename', file.filename)
        
        # Create temporary file to store uploaded content
        with tempfile.NamedTemporaryFile(delete=False, suffix=os.path.splitext(filename)[1]) as temp_file:
            file.save(temp_file.name)
            temp_file_path = temp_file.name
        
        try:
            # Convert file to markdown
            logger.info(f"Converting file: {filename}")
            result = markitdown.convert(temp_file_path)
            
            # Extract text content
            markdown_content = result.text_content if result else ""
            
            logger.info(f"Successfully converted {filename} to markdown ({len(markdown_content)} characters)")
            
            return jsonify({
                'success': True,
                'filename': filename,
                'markdown': markdown_content,
                'original_size': os.path.getsize(temp_file_path),
                'markdown_size': len(markdown_content)
            })
            
        finally:
            # Clean up temporary file
            try:
                os.unlink(temp_file_path)
            except OSError:
                pass
                
    except Exception as e:
        logger.error(f"Error converting file: {str(e)}")
        logger.error(traceback.format_exc())
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500

@app.route('/convert-url', methods=['POST'])
def convert_url():
    """
    Convert file from URL to markdown
    
    Expects:
    - JSON with 'url' field
    
    Returns:
    - JSON with markdown content or error message
    """
    try:
        data = request.get_json()
        
        if not data or 'url' not in data:
            return jsonify({'error': 'No URL provided'}), 400
        
        url = data['url']
        
        logger.info(f"Converting URL: {url}")
        result = markitdown.convert(url)
        
        markdown_content = result.text_content if result else ""
        
        logger.info(f"Successfully converted URL {url} to markdown ({len(markdown_content)} characters)")
        
        return jsonify({
            'success': True,
            'url': url,
            'markdown': markdown_content,
            'markdown_size': len(markdown_content)
        })
        
    except Exception as e:
        logger.error(f"Error converting URL: {str(e)}")
        logger.error(traceback.format_exc())
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500

@app.errorhandler(413)
def request_entity_too_large(error):
    return jsonify({'error': 'File too large'}), 413

@app.errorhandler(404)
def not_found(error):
    return jsonify({'error': 'Endpoint not found'}), 404

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5000))
    debug = os.environ.get('DEBUG', 'False').lower() == 'true'
    
    logger.info(f"Starting MarkitDown service on port {port}")
    app.run(host='0.0.0.0', port=port, debug=debug)
