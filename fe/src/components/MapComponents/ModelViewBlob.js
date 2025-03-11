// ModelViewerBlob.js
import React, { useEffect, useState } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { GLTFLoader } from "three/examples/jsm/loaders/GLTFLoader";
import * as THREE from "three";

const ModelViewerBlob = ({ modelSource, setCameraPosition, canvasStyle = { width: "100%", height: "400px" } }) => {
  const [model, setModel] = useState(null);
  const [sourceUrl, setSourceUrl] = useState(null);

  useEffect(() => {
    if (!modelSource) return;

    // Determine if modelSource is a string URL or a Blob.
    let url;
    if (typeof modelSource === "string") {
      url = modelSource;
    } else if (modelSource instanceof Blob) {
      url = URL.createObjectURL(modelSource);
    }
    setSourceUrl(url);

    return () => {
      // Clean up blob URL if created
      if (modelSource instanceof Blob && url) {
        URL.revokeObjectURL(url);
      }
    };
  }, [modelSource]);

  useEffect(() => {
    if (!sourceUrl) return;
    const loader = new GLTFLoader();
    loader.load(
      sourceUrl,
      (gltf) => {
        const scene = gltf.scene;
        console.log("Loaded model:", scene);
        setModel(scene);

        // Compute bounding box for proper centering and scaling.
        const box = new THREE.Box3().setFromObject(scene);
        const center = new THREE.Vector3();
        box.getCenter(center);
        const size = box.getSize(new THREE.Vector3());

        // Adjust position so model sits on the ground.
        scene.position.y -= box.min.y;

        // Normalize the scale.
        const maxDim = Math.max(size.x, size.y, size.z);
        const scaleFactor = 5 / maxDim;
        scene.scale.set(scaleFactor, scaleFactor, scaleFactor);

        // Adjust camera using the computed center and dimensions.
        const newCameraPosition = [
          center.x,
          center.y + size.y * 1.5,
          center.z + size.z * 2,
        ];
        if (setCameraPosition) {
          setCameraPosition(newCameraPosition);
        }
      },
      undefined,
      (error) => console.error("Error loading model:", error)
    );
  }, [sourceUrl, setCameraPosition]);

  return (
    <div style={canvasStyle}>
      <Canvas camera={{ position: [0, 5, 15], fov: 50 }}>
        <ambientLight intensity={1} />
        <directionalLight position={[10, 20, 10]} intensity={1.5} />
        {/* <axesHelper args={[5]} /> */}
        {/* <gridHelper args={[10, 10]} /> */}
        {model && <primitive object={model} />}
        <OrbitControls minDistance={2} maxDistance={100} enableDamping dampingFactor={0.1} rotateSpeed={0.5} />
      </Canvas>
    </div>
  );
};

export default ModelViewerBlob;
