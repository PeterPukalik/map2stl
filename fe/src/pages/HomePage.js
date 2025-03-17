import React, { useEffect, useState, useRef } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { GLTFLoader } from "three/examples/jsm/loaders/GLTFLoader";
import * as THREE from "three";
import axios from "axios";
import "leaflet/dist/leaflet.css";
import { MapContainer, TileLayer, Rectangle, useMapEvents } from "react-leaflet";

// Child component for loading and displaying the GLB model
const Model = ({ modelUrl, setCameraPosition }) => {
  const [model, setModel] = useState(null);

  useEffect(() => {
    if (!modelUrl) return;

    const loader = new GLTFLoader();
    loader.load(
      modelUrl,
      (gltf) => {
        const scene = gltf.scene;
        console.log("Loaded model:", scene);
        setModel(scene);

        // Compute bounding box
        const box = new THREE.Box3().setFromObject(scene);
        const center = new THREE.Vector3();
        box.getCenter(center);
        const size = box.getSize(new THREE.Vector3());

        // Move model up so the bottom sits on Y=0
        scene.position.y -= box.min.y;

        // Optional: Normalize scale so it's not too large in the scene
        const maxDim = Math.max(size.x, size.y, size.z);
        const scaleFactor = 5 / maxDim;
        scene.scale.set(scaleFactor, scaleFactor, scaleFactor);

        // Position camera (optional)
        const newCameraPosition = [
          center.x,
          center.y + size.y * 1.5,
          center.z + size.z * 2,
        ];
        setCameraPosition(newCameraPosition);
      },
      undefined,
      (error) => console.error("Error loading model:", error)
    );
  }, [modelUrl, setCameraPosition]);

  return model ? <primitive object={model} /> : null;
};

// Handles map clicks & updates bounding box
const MapEvents = ({ setBoundingBox }) => {
  useMapEvents({
    click(e) {
      const lat = e.latlng.lat;
      const lng = e.latlng.lng;
      // Example: create a 0.1° x 0.1° bounding box around the click
      setBoundingBox([
        [lat - 0.05, lng - 0.05],
        [lat + 0.05, lng + 0.05],
      ]);
    },
  });
  return null;
};

const HomePage = () => {
  // States
  const [modelUrl, setModelUrl] = useState(null);
  const [loading, setLoading] = useState(false);
  const [boundingBox, setBoundingBox] = useState([
    [49.2, 18.6], // top-left
    [49.3, 18.7], // bottom-right
  ]);
  const [cameraPosition, setCameraPosition] = useState([0, 5, 15]);

  // Sliders
  const [zFactor, setZFactor] = useState(1);       // Range 1..10
  const [meshReduce, setMeshReduce] = useState(0.5); // Range 0..1

  // Orbit controls ref
  const controlsRef = useRef();

  // Generate the model via API
  const generateModel = async () => {
    setLoading(true);
    try {
      const response = await axios.post("https://localhost:7188/api/generate-model", {
        boundingBox,
        zFactor,
        meshReduce,
      });
      console.log("API Response:", response.data);

      if (response.data && response.data.fileUrl) {
        setModelUrl(`https://localhost:7188${response.data.fileUrl}`);
      } else {
        console.error("Model URL not found in response");
      }
    } catch (error) {
      console.error("Error fetching model URL:", error);
    }
    setLoading(false);
  };

  return (
    <div style={{ display: "flex", flexDirection: "column", height: "100vh" }}>
      {/* Top section: Map on the left, sliders + button on the right */}
      <div style={{ display: "flex", flexDirection: "row", height: "400px" }}>
        {/* Map container */}
        <div style={{ flex: 3, position: "relative" }}>
          <h2 style={{ textAlign: "center" }}>Select an area for 3D model</h2>
          <MapContainer center={[49.2, 18.6]} zoom={13} style={{ width: "100%", height: "80%" }}>
            <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
            <Rectangle bounds={boundingBox} color="blue" />
            <MapEvents setBoundingBox={setBoundingBox} />
          </MapContainer>
        </div>

        {/* Sliders + generate button */}
        <div style={{ flex: 2, padding: "10px" }}>
          <h3>Model Settings</h3>

          <div style={{ marginBottom: "20px" }}>
            <label style={{ marginRight: "10px" }}>Z Factor: {zFactor}</label>
            <input
              type="range"
              min="1"
              max="10"
              step="1"
              value={zFactor}
              onChange={(e) => setZFactor(Number(e.target.value))}
            />
          </div>

          <div style={{ marginBottom: "20px" }}>
            <label style={{ marginRight: "10px" }}>Mesh Reduction: {meshReduce}</label>
            <input
              type="range"
              min="0"
              max="1"
              step="0.1"
              value={meshReduce}
              onChange={(e) => setMeshReduce(Number(e.target.value))}
            />
          </div>

          <button
            onClick={generateModel}
            disabled={loading}
            style={{ padding: "10px 20px", fontSize: "16px" }}
          >
            {loading ? "Generating..." : "Generate 3D Model"}
          </button>
        </div>
      </div>

      {/* Bottom section: 3D Model preview */}
      <div style={{ flex: 1, background: "#f0f0f0" }}>
        <h2 style={{ textAlign: "center" }}>3D Model Preview</h2>
        <Canvas camera={{ position: cameraPosition, fov: 50 }}>
          <ambientLight intensity={1} />
          <directionalLight position={[10, 20, 10]} intensity={1.5} />
          <axesHelper args={[5]} />
          <gridHelper args={[10, 10]} />

          {modelUrl && <Model modelUrl={modelUrl} setCameraPosition={setCameraPosition} />}

          <OrbitControls
            ref={controlsRef}
            minDistance={2}
            maxDistance={100}
            enableDamping={true}
            dampingFactor={0.1}
            rotateSpeed={0.5}
          />
        </Canvas>
      </div>
    </div>
  );
};

export default HomePage;
