#!/usr/bin/env python3
import os
import glob
import json
import datetime
import sys

# Check dependencies
try:
    import matplotlib.pyplot as plt
except ImportError:
    print("Error: matplotlib is not installed or failed to load.")
    print("Install it via: pip install matplotlib")
    sys.exit(1)

def load_data(json_dir):
    data = {}
    for filename in glob.glob(os.path.join(json_dir, '*.json')):
        try:
            mtime = os.path.getmtime(filename)
            run_date = datetime.datetime.fromtimestamp(mtime)
        except Exception:
            continue
        with open(filename, 'r') as f:
            obj = json.load(f)
        benches = obj.get('Benchmarks', [])
        for b in benches:
            name = b.get('MethodTitle') or b.get('DisplayInfo') or b.get('FullName', '<unknown>')
            stats = b.get('Statistics', {})
            mean = stats.get('Mean')
            if mean is None:
                continue
            data.setdefault(name, []).append((run_date, mean))
    return data

def plot_data(data):
    plt.figure()
    for name, points in data.items():
        points.sort(key=lambda x: x[0])
        dates = [pt[0] for pt in points]
        means = [pt[1] for pt in points]
        plt.plot(dates, means, label=name)
    plt.xlabel('Run Date')
    plt.ylabel('Mean Time (ns)')
    plt.title('Benchmark Trends')
    plt.legend()
    plt.tight_layout()
    output = 'benchmark_trends.png'
    plt.savefig(output)
    print(f"Chart saved to {output}")
    plt.show()

if __name__ == '__main__':
    json_dir = os.path.dirname(os.path.abspath(__file__))
    data = load_data(json_dir)
    if not data:
        print('No JSON files found in', json_dir)
    else:
        plot_data(data)
