'use strict';

const fs = require('node:fs');
const path = require('node:path');
const source = process.argv[2] || path.resolve(__dirname, '..', '..', 'gcbh_dynamic_campaign', 'mission-map', 'renderer', 'data', 'global-land.geojson');
const output = path.resolve(__dirname, '..', 'AlwaysFaithful', 'Assets', 'Resources', 'Geography', 'taiwan-coastline.json');
const bounds = { west: 119.75, east: 122.2, south: 21.7, north: 25.4 };

function clip(ring, inside, intersect) {
  const result = [];
  for (let index = 0; index < ring.length; index += 1) {
    const current = ring[index], previous = ring[(index + ring.length - 1) % ring.length];
    const currentInside = inside(current), previousInside = inside(previous);
    if (currentInside) {
      if (!previousInside) result.push(intersect(previous, current));
      result.push(current);
    } else if (previousInside) result.push(intersect(previous, current));
  }
  return result;
}

function clipBounds(input) {
  let ring = input.slice(0, input.length > 1 && input[0][0] === input.at(-1)[0] && input[0][1] === input.at(-1)[1] ? -1 : undefined);
  ring = clip(ring, ([x]) => x >= bounds.west, (a, b) => [bounds.west, a[1] + (b[1] - a[1]) * (bounds.west - a[0]) / (b[0] - a[0])]);
  ring = clip(ring, ([x]) => x <= bounds.east, (a, b) => [bounds.east, a[1] + (b[1] - a[1]) * (bounds.east - a[0]) / (b[0] - a[0])]);
  ring = clip(ring, ([, y]) => y >= bounds.south, (a, b) => [a[0] + (b[0] - a[0]) * (bounds.south - a[1]) / (b[1] - a[1]), bounds.south]);
  ring = clip(ring, ([, y]) => y <= bounds.north, (a, b) => [a[0] + (b[0] - a[0]) * (bounds.north - a[1]) / (b[1] - a[1]), bounds.north]);
  return ring;
}

function perpendicular(point, start, end) {
  const dx = end[0] - start[0], dy = end[1] - start[1];
  if (dx === 0 && dy === 0) return Math.hypot(point[0] - start[0], point[1] - start[1]);
  const t = Math.max(0, Math.min(1, ((point[0] - start[0]) * dx + (point[1] - start[1]) * dy) / (dx * dx + dy * dy)));
  return Math.hypot(point[0] - (start[0] + t * dx), point[1] - (start[1] + t * dy));
}

function simplify(points, tolerance) {
  if (points.length <= 3) return points;
  let farthest = 0, split = 0;
  for (let index = 1; index < points.length - 1; index += 1) {
    const distance = perpendicular(points[index], points[0], points.at(-1));
    if (distance > farthest) { farthest = distance; split = index; }
  }
  if (farthest <= tolerance) return [points[0], points.at(-1)];
  return [...simplify(points.slice(0, split + 1), tolerance).slice(0, -1), ...simplify(points.slice(split), tolerance)];
}

const geojson = JSON.parse(fs.readFileSync(source, 'utf8'));
const polygons = [];
for (const feature of geojson.features || []) {
  const sets = feature.geometry?.type === 'Polygon' ? [feature.geometry.coordinates] : feature.geometry?.type === 'MultiPolygon' ? feature.geometry.coordinates : [];
  for (const rings of sets) {
    const outer = rings?.[0];
    if (!outer?.length) continue;
    const extent = outer.reduce((v, [x, y]) => ({ west: Math.min(v.west, x), east: Math.max(v.east, x), south: Math.min(v.south, y), north: Math.max(v.north, y) }), { west: Infinity, east: -Infinity, south: Infinity, north: -Infinity });
    if (extent.east < bounds.west || extent.west > bounds.east || extent.north < bounds.south || extent.south > bounds.north) continue;
    let clipped = clipBounds(outer);
    if (clipped.length < 3) continue;
    clipped = simplify([...clipped, clipped[0]], 0.002).slice(0, -1);
    if (clipped.length >= 3) polygons.push({ points: clipped.map(([longitude, latitude]) => ({ longitude, latitude })) });
  }
}

const result = {
  metadata: { title: 'Taiwan Natural Earth 1:10m map crop', source: 'Local GCBH cached Natural Earth 1:10m land and minor islands', sourceFile: source, license: 'Natural Earth public domain' },
  ...bounds,
  polygons,
};
fs.mkdirSync(path.dirname(output), { recursive: true });
fs.writeFileSync(output, `${JSON.stringify(result)}\n`, 'utf8');
console.log(`Wrote ${polygons.length} coastline polygons to ${output} (${fs.statSync(output).size} bytes).`);
