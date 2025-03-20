import asyncio
import edge_tts

async def main():
    # Select a voice (you can change this to other available voices)
    voice = "ja-JP-NanamiNeural"
    
    # Text you want to convert to speech
    text = "手伝ってくれてありがとう、Violet。実は、Emilyに告白しようと思っているんだ。"
    
    # Create a communication object
    communicate = edge_tts.Communicate(text, voice)
    
    # Convert to speech and save as an audio file
    await communicate.save("output.mp3")
    
    print(f"Speech saved as 'output.mp3'")
    
    # List available voices (optional)
    # voices = await edge_tts.list_voices()
    # print("\nSome available voices:")
    # for i, voice_option in enumerate(voices[:5]):  # Show only first 5 voices
    #     print(f"{i+1}. {voice_option['ShortName']} - {voice_option['LocaleName']}") # type: ignore

if __name__ == "__main__":
    # Run the async function
    asyncio.run(main())
