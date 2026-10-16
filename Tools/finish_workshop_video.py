"""Package an actual WorkshopRecorder take with Korean subtitles and captured audio.

python Tools/finish_workshop_video.py Logs/workshop-video-take01 --ffmpeg C:/path/ffmpeg.exe
No generated gameplay frames or altered gameplay timings are used.
"""
import argparse
import json
import re
import shutil
import subprocess
from pathlib import Path


def seconds(stamp):
    h, m, s = stamp.replace(',', '.').split(':')
    return int(h) * 3600 + int(m) * 60 + float(s)


def ass_time(t):
    centis = round(t * 100)
    return f'{centis // 360000}:{centis // 6000 % 60:02}:{centis // 100 % 60:02}.{centis % 100:02}'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('take', type=Path)
    parser.add_argument('--ffmpeg', required=True, type=Path)
    parser.add_argument('--out', type=Path, default=Path('Videos'))
    args = parser.parse_args()
    take, out = args.take.resolve(), args.out.resolve()
    result = json.loads((take / 'complete.json').read_text(encoding='utf-8-sig'))
    if (take / 'error.txt').exists():
        raise RuntimeError((take / 'error.txt').read_text(encoding='utf-8-sig'))
    if result['customAugmentRank'] < 1:
        raise RuntimeError('The custom augment was not selected during real gameplay.')
    duration = result['frames'] / result['fps']
    audio_duration = (take / 'audio.f32').stat().st_size / (4 * 2 * result['sampleRate'])
    if abs(audio_duration - duration) > 0.1:
        raise RuntimeError(f'Audio/video capture differs: {audio_duration} vs {duration}')
    out.mkdir(parents=True, exist_ok=True)
    subtitles = (take / 'subtitles.srt').read_text(encoding='utf-8-sig')
    chapters = [line.split('\t', 1) for line in (take / 'chapters.tsv').read_text(encoding='utf-8-sig').splitlines()]
    normalized = []
    # Chapter timestamps are authoritative, including takes made before the cue-flush fix.
    for block in re.split(r'\n\s*\n', subtitles.strip()):
        lines = block.splitlines()
        start = seconds(lines[1].split(' --> ')[0])
        lines[2] = next(title for at, title in reversed(chapters) if float(at) <= start + 0.002)
        normalized.append('\n'.join(lines))
    subtitles = '\n\n'.join(normalized) + '\n'
    ass = '''[Script Info]
ScriptType: v4.00+
PlayResX: 1920
PlayResY: 1080
WrapStyle: 2
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,Malgun Gothic,30,&H00F4F5F7,&H000000FF,&H00170D08,&H00170D08,0,0,0,0,100,100,0,0,1,1,0,2,100,100,22,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
    for block in re.split(r'\n\s*\n', subtitles.strip()):
        lines = block.splitlines()
        start, end = lines[1].split(' --> ')
        text = r'{\b1\fs25\c&H00E5D64B&}' + lines[2] + r'{\r}\N' + r'\N'.join(lines[3:])
        ass += f'Dialogue: 0,{ass_time(seconds(start))},{ass_time(seconds(end))},Default,,0,0,0,,{text}\n'
    (take / 'subtitles.ass').write_text(ass, encoding='utf-8-sig')
    metadata = ';FFMETADATA1\ntitle=REBOUND — MOD 제작부터 설치 플레이까지\nartist=REBOUND project\n'
    for i, (start, title) in enumerate(chapters):
        end = float(chapters[i+1][0]) if i+1 < len(chapters) else duration
        metadata += f'[CHAPTER]\nTIMEBASE=1/1000\nSTART={round(float(start)*1000)}\nEND={round(end*1000)}\ntitle={title}\n'
    (take / 'chapters.ffmeta').write_text(metadata, encoding='utf-8')
    movie = out / 'REBOUND-MOD-Workshop-KO.mp4'
    command = [str(args.ffmpeg.resolve()), '-y', '-hide_banner',
        '-i', 'video.mp4', '-f', 'f32le', '-ar', str(result['sampleRate']), '-ac', '2', '-i', 'audio.f32',
        '-i', 'chapters.ffmeta', '-map', '0:v:0', '-map', '1:a:0', '-map_metadata', '2', '-map_chapters', '2',
        '-vf', 'pad=1920:1080:160:20:color=0x080d17,ass=subtitles.ass',
        '-c:v', 'libx264', '-crf', '18', '-preset', 'medium', '-pix_fmt', 'yuv420p',
        '-c:a', 'aac', '-b:a', '192k', '-t', str(duration), '-movflags', '+faststart', str(movie)]
    with (take / 'final-encode.log').open('w', encoding='utf-8') as log:
        subprocess.run(command, cwd=take, stdout=log, stderr=subprocess.STDOUT, check=True)
    (out / 'REBOUND-MOD-Workshop-KO.srt').write_text(subtitles, encoding='utf-8-sig')
    shutil.copyfile(take / 'neon-wave-lab.zip', out / 'neon-wave-lab.zip')
    print(json.dumps({'video': str(movie), 'duration': duration, 'audioDuration': audio_duration,
                      'chapters': chapters, 'bytes': movie.stat().st_size}, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
