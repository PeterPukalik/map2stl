import React, { useState } from 'react';
import { MapContainer, TileLayer, FeatureGroup } from 'react-leaflet';
import { EditControl } from 'react-leaflet-draw';
import 'leaflet/dist/leaflet.css';
import 'leaflet-draw/dist/leaflet.draw.css';
import './MapComponent.css';

function MapWithDraw() {
  const [bounds, setBounds] = useState(null);

  // Handler for when a shape is created
  const onCreated = (e) => {
    const { layerType, layer } = e;

    if (layerType === 'rectangle') {
      // rectangle has a getBounds() method
      const rectangleBounds = layer.getBounds();

      // getBounds() returns an object with getSouthWest(), getNorthEast(), etc.
      const sw = rectangleBounds.getSouthWest();
      const ne = rectangleBounds.getNorthEast();

      // Save latlng corners in state
      setBounds({
        southwest: { lat: sw.lat, lng: sw.lng },
        northeast: { lat: ne.lat, lng: ne.lng },
      });
    }
    // If you need polygons or circles, handle them here as well
  };

  // Example function to send bounding box to backend
  const sendToBackend = () => {
    if (!bounds) return;
    // fetch/axios to your API
    // e.g. axios.post('/api/create-stl', bounds)
    console.log('Sending bounding box to backend:', bounds);
  };

  return (
    <div>
        {/* <MapContainer center={[48.673, 19.699]} zoom={7} style={{ width: '600px', height: '400px' }}> */}
        <MapContainer className="map-container" center={[48.673, 19.699]} zoom={7} >
        <TileLayer
          attribution="&copy; OpenStreetMap contributors"
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <FeatureGroup>
          <EditControl
            position="topright"
            draw={{
              polyline: false,
              polygon: false,
              circle: false,
              circlemarker: false,
              marker: false,
              rectangle: true, // Enable the rectangle draw tool
            }}
            edit={{
              remove: true,
            }}
            onCreated={onCreated}
          />
        </FeatureGroup>
      </MapContainer>

      <button onClick={sendToBackend} disabled={!bounds}>
        Generate STL
      </button>

      {bounds && (
        <p>
          Southwest: ({bounds.southwest.lat.toFixed(4)}, {bounds.southwest.lng.toFixed(4)})<br />
          Northeast: ({bounds.northeast.lat.toFixed(4)}, {bounds.northeast.lng.toFixed(4)})
        </p>
      )}
    </div>
  );
}

export default MapWithDraw;
