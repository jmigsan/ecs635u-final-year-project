#!/usr/bin/env python3
import asyncio
import json
import websockets
from uuid import uuid4

# Set your client ID, or generate a random one
CLIENT_ID = "Eugene"

# Server URL
SERVER_URL = f"ws://localhost:8000/ws/{CLIENT_ID}"

# Hardcoded responses for different actions
async def handle_action(action_data):
    """Generate response based on the action request from the server"""
    action = action_data.get("action")
    action_id = action_data.get("action_id")
    args = action_data.get("args")
    
    response = {
        "action_id": action_id,
        "status": "success"
    }
    
    if action == "walk":
        target = args
        print(f"Server asked to walk to: {target}")
        response["new_state"] = f"You are 0.5 meters away from {target}."
    
    elif action == "talk":
        if isinstance(args, dict):
            target = args.get("target")
            message = args.get("message")
            print(f"Server asked to talk to {target} with message: {message}")
            
            # Simulate different responses based on who we're talking to
            if target.lower() == "gerard":
                response["message"] = "Gerard smiled and replied, 'Hello there! It's a beautiful day, isn't it?'"
            elif target.lower() == "felix":
                response["message"] = "Felix waved while flying his kite, 'Hey! Want to try flying this kite?'"
            elif target.lower() == "diana":
                response["message"] = "Diana nodded politely while watching her child play."
            else:
                response["message"] = f"{target} acknowledged your message."
    
    # Add a small delay to simulate processing time
    await asyncio.sleep(1)
    return response

async def client():
    """Main client function to handle the WebSocket connection"""
    print(f"Connecting to {SERVER_URL} with client ID: {CLIENT_ID}")
    
    async with websockets.connect(SERVER_URL) as websocket:
        print("Connected to server!")
        
        # Main client loop
        while True:
            try:
                # Wait for messages from the server
                message = await websocket.recv()
                print(f"Received: {message}")
                
                # Parse the message
                try:
                    data = json.loads(message)
                    
                    # Process the action and generate a response
                    if "action" in data and "action_id" in data:
                        response = await handle_action(data)
                        
                        # Send the response back to the server
                        await websocket.send(json.dumps(response))
                        print(f"Sent response: {response}")
                    
                except json.JSONDecodeError:
                    print(f"Received non-JSON message: {message}")
                
            except websockets.exceptions.ConnectionClosed:
                print("Connection closed")
                break

# Run the client
if __name__ == "__main__":
    try:
        asyncio.run(client())
    except KeyboardInterrupt:
        print("\nClient stopped by user")
    except Exception as e:
        print(f"Error: {e}")
