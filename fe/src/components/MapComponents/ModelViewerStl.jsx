// ModelViewerStl.jsx
import React, { useEffect, useState } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { STLLoader } from "three/examples/jsm/loaders/STLLoader";
import * as THREE from "three";

const ModelViewerStl = ({ modelSource, setCameraPosition, canvasStyle = { width: "100%", height: "400px" } }) => {
  const [mesh, setMesh] = useState(null);
  const [sourceUrl, setSourceUrl] = useState(null);

  // Create a URL from the modelSource if necessary.
  useEffect(() => {
    if (!modelSource) return;
    let url;
    if (typeof modelSource === "string") {
      url = modelSource;
    } else if (modelSource instanceof Blob) {
      url = URL.createObjectURL(modelSource);
    }
    setSourceUrl(url);

    return () => {
      if (modelSource instanceof Blob && url) {
        URL.revokeObjectURL(url);
      }
    };
  }, [modelSource]);

  // Load the STL using STLLoader.
  useEffect(() => {
    if (!sourceUrl) return;
    const loader = new STLLoader();
    loader.load(
      sourceUrl,
      (geometry) => {
        // Create a basic material.
        const material = new THREE.MeshStandardMaterial({ color: 0xcccccc });
        const loadedMesh = new THREE.Mesh(geometry, material);

        // Compute the bounding box.
        geometry.computeBoundingBox();
        if (geometry.boundingBox) {
          const box = geometry.boundingBox;

          // Get the center of the bounding box.
          const center = new THREE.Vector3();
          box.getCenter(center);
          // Apply a single rotation of +90° (PI/2) about the X axis.
          loadedMesh.rotation.x = -Math.PI / 2;
          // Shift the mesh so that its center is at the origin.
        //   loadedMesh.position.sub(center);
          // Update the world matrix so we can get the new bounding box.
          loadedMesh.updateMatrixWorld(true);
          const newBox = new THREE.Box3().setFromObject(loadedMesh);

          // Shift upward so that the lowest point is at y = 0.
        //   loadedMesh.position.y -= newBox.min.y;

          // Optionally, scale the mesh if needed. For example, if you find the model too small:
          const size = new THREE.Vector3();
          newBox.getSize(size);
          const maxDim = Math.max(size.x, size.y, size.z);
          const scaleFactor = 5 / maxDim; // Adjust '5' as needed.
          loadedMesh.scale.set(scaleFactor, scaleFactor, scaleFactor);
          loadedMesh.updateMatrixWorld(true);
          
          // Calculate a suggested camera position.
          if (setCameraPosition) {
            const finalBox = new THREE.Box3().setFromObject(loadedMesh);
            const finalSize = new THREE.Vector3();
            finalBox.getSize(finalSize);
            // Position the camera above and slightly back.
            const suggestedCamPos = [
              0,
              finalSize.y * 1.5,
              finalSize.z * 2,
            ];
            setCameraPosition(suggestedCamPos);
          }
        }

        setMesh(loadedMesh);
      },
      undefined,
      (error) => {
        console.error("Error loading STL file:", error);
      }
    );
  }, [sourceUrl, setCameraPosition]);

  return (
    <div style={canvasStyle}>
      <Canvas camera={{ position: [0, 5, 15], fov: 50 }}>
        <ambientLight intensity={1} />
        <directionalLight position={[10, 20, 10]} intensity={1.5} />
        {mesh && <primitive object={mesh} />}
        {/* <axesHelper args={[5]} />
         <gridHelper args={[10, 10]} />  */}
        <OrbitControls minDistance={2} maxDistance={1000} enableDamping dampingFactor={0.1} rotateSpeed={0.5} />
      </Canvas>
    </div>
  );
};

export default ModelViewerStl;
