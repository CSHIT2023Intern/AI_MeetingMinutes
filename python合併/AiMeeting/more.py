import azure.cognitiveservices.speech as speechsdk
import time
import uuid
from scipy.io import wavfile

speech_key, service_region = "your-subscription-key", "your-region"
# 8 channel, 16 bits, 16kHz audio
meetingfilename = "audio-file-to-transcribe.wav"


def meeting_transcription_differentiate_speakers():

    speech_config = speechsdk.SpeechConfig(
        subscription=speech_key, region=service_region)
    speech_config.set_property_by_name(
        "MeetingTranscriptionInRoomAndOnline", "true")
    speech_config.set_property_by_name("DifferentiateGuestSpeakers", "true")

    channels = 8
    bits_per_sample = 16
    samples_per_second = 16000

    wave_format = speechsdk.audio.AudioStreamFormat(
        samples_per_second, bits_per_sample, channels)
    stream = speechsdk.audio.PushAudioInputStream(stream_format=wave_format)
    audio_config = speechsdk.audio.AudioConfig(stream=stream)

    transcriber = speechsdk.transcription.MeetingTranscriber(audio_config)

    meeting_id = str(uuid.uuid4())
    meeting = speechsdk.transcription.Meeting(speech_config, meeting_id)
    done = False

    def stop_cb(evt: speechsdk.SessionEventArgs):
        """callback that signals to stop continuous transcription upon receiving an event `evt`"""
        print('CLOSING {}'.format(evt))
        nonlocal done
        done = True

    transcriber.transcribed.connect(
        lambda evt: print('TRANSCRIBED: {}'.format(evt)))
    transcriber.session_started.connect(
        lambda evt: print('SESSION STARTED: {}'.format(evt)))
    transcriber.session_stopped.connect(
        lambda evt: print('SESSION STOPPED {}'.format(evt)))
    transcriber.canceled.connect(lambda evt: print('CANCELED {}'.format(evt)))
    # stop continuous transcription on either session stopped or canceled events
    transcriber.session_stopped.connect(stop_cb)
    transcriber.canceled.connect(stop_cb)

    # Note user voice signatures are not required for speaker differentiation.
    # Use voice signatures when adding participants when more enhanced speaker identification is required.
    user1 = speechsdk.transcription.Participant(
        "user1@example.com", "en-us", voice_signature_user1)
    user2 = speechsdk.transcription.Participant(
        "user2@example.com", "en-us", voice_signature_user2)

    meeting.add_participant_async(user1).get()
    meeting.add_participant_async(user2).get()
    transcriber.join_meeting_async(meeting).get()
    transcriber.start_transcribing_async()

    sample_rate, wav_data = wavfile.read(meetingfilename)
    stream.write(wav_data.tobytes())
    stream.close()
    while not done:
        time.sleep(.5)

    transcriber.stop_transcribing_async()
