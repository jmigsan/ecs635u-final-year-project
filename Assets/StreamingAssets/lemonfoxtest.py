# pip install requests
import requests
import os
from dotenv import load_dotenv
load_dotenv()

url = "https://api.lemonfox.ai/v1/audio/transcriptions"

api_key = os.getenv("LEMONFOX_API_KEY")
headers = {
    "Authorization": f"Bearer {api_key}"
}

files = {"file": open("output.mp3", "rb")}

data = {
  "language": "english",
  "response_format": "json"
}

response = requests.post(url, headers=headers, files=files, data=data)
print(response.json())

# To upload a local file add the files parameter:
# files = {"file": open("/path/to/audio.mp3", "rb")}
# response = requests.post(url, headers=headers, files=files, data=data)