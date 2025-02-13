from llama_cpp import Llama

def load_model(model_path):
    # Initialize the LLM with GPU acceleration
    llm = Llama(
        model_path=model_path,
        n_ctx=2048,  # Context window size
        n_threads=8,  # CPU threads
        n_gpu_layers=-1,  # Layers to offload to GPU (adjust based on your VRAM)
        verbose=False  # Set to True for debugging
    )
    return llm

def format_prompt(user_input, history):
    # Format the conversation history with system prompt
    system_prompt = "You are a helpful AI assistant. Respond conversationally."
    prompt = f"[INST] <<SYS>>\n{system_prompt}\n<</SYS>>\n\n"
    
    # Add conversation history
    for msg in history[-5:]:  # Keep last 5 exchanges
        prompt += f"{msg['role']}: {msg['content']}\n"
    
    # Add current user input
    prompt += f"User: {user_input}\nAssistant: [/INST]"
    return prompt

def main():
    # Configuration
    MODEL_PATH = r"E:\Code\AppData\LM-Studio\Models\bartowski\Falcon3-7B-Instruct-abliterated-GGUF\Falcon3-7B-Instruct-abliterated-Q5_K_L.gguf"  # Replace with your GGUF file
    
    # Initialize
    llm = load_model(MODEL_PATH)
    conversation_history = []
    
    print("Chatbot initialized. Type 'exit' to end the conversation.")
    
    while True:
        # Get user input
        user_input = input("\nYou: ")
        
        if user_input.lower() == 'exit':
            break
        
        # Create prompt with history
        full_prompt = format_prompt(user_input, conversation_history)
        print("hey nerd\n\n" + full_prompt)
        
        # Generate response
        response = llm.create_chat_completion(
            messages=[{"role": "user", "content": full_prompt}],
            temperature=0.7,
            max_tokens=256,
            stop=["User:", "Assistant:"],
            stream=False
        )
        
        # Extract and display response
        bot_response = response['choices'][0]['message']['content'].strip()
        print(f"\nAssistant: {bot_response}")
        
        # Update conversation history
        conversation_history.append({"role": "user", "content": user_input})
        conversation_history.append({"role": "assistant", "content": bot_response})

if __name__ == "__main__":
    main()