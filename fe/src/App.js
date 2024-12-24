import React, { useState } from 'react';
import MapComponent from './components/MapComponent';
import ThreeDComponent from './components/ThreeDComponent';

function App() {
  const [selectedArea, setSelectedArea] = useState(null);

  const handleAreaSelect = (bounds) => {
    setSelectedArea(bounds);
  };

  return (
    <div className="App">
      <h1>Map to 3D Model Converter</h1>
      <MapComponent onAreaSelect={handleAreaSelect} />
      {selectedArea && <ThreeDComponent areaBounds={selectedArea} />}
    </div>
  );
}

export default App;


