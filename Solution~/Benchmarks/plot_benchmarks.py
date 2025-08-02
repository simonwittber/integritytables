#!/usr/bin/env python3
import os, glob, json, datetime
import plotly.express as px

def load_data(json_dir):
    data = {}
    for fn in glob.glob(os.path.join(json_dir, '*.json')):
        run_date = datetime.datetime.fromtimestamp(os.path.getmtime(fn))
        with open(fn) as f:
            obj = json.load(f)
        for b in obj.get('Benchmarks', []):
            name = b.get('MethodTitle') or b.get('DisplayInfo')
            mean = b.get('Statistics', {}).get('Mean')
            if mean is None: continue
            data.setdefault(name, []).append((run_date, mean))
    return data

def plot_data(data):
    # flatten into a list of dicts
    rows = []
    for name, pts in data.items():
        for date, mean in pts:
            rows.append({'Benchmark': name, 'Date': date, 'Mean(ns)': mean})
    fig = px.line(
        rows, x='Date', y='Mean(ns)', color='Benchmark',
        title='Benchmark Trends', markers=True
    )
    out = 'benchmark_trends.html'
    fig.write_html(out, auto_open=True)
    print(f"Written interactive HTML → {out}")


if __name__ == '__main__':
    json_dir = os.path.dirname(os.path.abspath(__file__))
    json_dir = os.path.join(json_dir, 'BenchmarkDotNet.Artifacts/results')
    data = load_data(json_dir)
    if not data:
        print('No JSON files found in', json_dir)
    else:
        plot_data(data)
