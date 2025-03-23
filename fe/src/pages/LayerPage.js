// LayerPage.jsx
import React, { useRef, useEffect, useState } from "react";
import * as THREE from "three";
import { GLTFLoader } from "three/examples/jsm/loaders/GLTFLoader.js";
import { mergeGroups } from "three/examples/jsm/utils/BufferGeometryUtils.js";
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";
import { STLExporter } from "three/examples/jsm/exporters/STLExporter.js";

// If your GLB files are in the same folder, import them (or use /path if in public)
import demModelPath from "./model.glb";
import osmModelPath from "./osm.glb";

// ---
// Helper function: shrinkwrapBuildingLayer
// This function takes a THREE.Mesh (assumed to be part of the OSM layer) and a terrain object,
// and for each vertex in the mesh that is near the building’s base (local Y near the minimum),
// it casts a downward ray against the terrain and updates that vertex’s Y so it "hugs" the terrain.
function shrinkwrapBuildingLayer(buildingMesh, terrain, raycaster, threshold = 0.1) {
  buildingMesh.updateMatrixWorld(true);
  terrain.updateMatrixWorld(true);

  // Gather terrain meshes (all sub-meshes of terrain)
  const terrainMeshes = [];
  terrain.traverse(child => {
    if (child.isMesh) terrainMeshes.push(child);
  });
  
  // Get the geometry (assume BufferGeometry)
  const geom = buildingMesh.geometry;
  if (!geom || !geom.isBufferGeometry) {
    console.warn("Mesh does not use BufferGeometry.");
    return;
  }
  
  const posAttr = geom.attributes.position;
  // First, find the local minimum Y of this geometry
  let minY = Infinity;
  for (let i = 0; i < posAttr.count; i++) {
    const y = posAttr.getY(i);
    if (y < minY) minY = y;
  }
  
  // For each vertex that is within 'threshold' of the minimum, adjust its Y based on the terrain.
  // We convert from local to world, perform a raycast, then convert back.
  const tempWorld = new THREE.Vector3();
  const tempLocal = new THREE.Vector3();
  const matrixWorld = buildingMesh.matrixWorld;
  const invMatrixWorld = new THREE.Matrix4().copy(matrixWorld).invert();
  const down = new THREE.Vector3(0, -1, 0);
  
  for (let i = 0; i < posAttr.count; i++) {
    const vx = posAttr.getX(i);
    const vy = posAttr.getY(i);
    const vz = posAttr.getZ(i);
    
    // Only process vertices near the base
    if (Math.abs(vy - minY) <= threshold) {
      tempLocal.set(vx, vy, vz);
      // Convert to world space
      tempWorld.copy(tempLocal).applyMatrix4(matrixWorld);
      // Start ray just a bit above the vertex to avoid self-intersection.
      const rayOrigin = tempWorld.clone();
      rayOrigin.y += 0.1;
      
      raycaster.set(rayOrigin, down);
      const intersects = raycaster.intersectObjects(terrainMeshes, true);
      if (intersects.length > 0) {
        // Use the first intersection.
        const terrainY = intersects[0].point.y;
        // If terrain is higher than the current vertex world position, update it.
        if (terrainY > tempWorld.y) {
          tempWorld.y = terrainY;
          // Convert back to local space.
          tempLocal.copy(tempWorld).applyMatrix4(invMatrixWorld);
          posAttr.setXYZ(i, tempLocal.x, tempLocal.y, tempLocal.z);
        }
      }
    }
  }
  posAttr.needsUpdate = true;
  geom.computeVertexNormals();
}

function LayerPage() {
  const containerRef = useRef(null);
  const [cameraPosition] = useState([0, 5, 15]);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;
    
    // Create Scene, Camera, Renderer
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(50, container.clientWidth / container.clientHeight, 0.1, 100000);
    camera.position.set(...cameraPosition);
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(container.clientWidth, container.clientHeight);
    container.appendChild(renderer.domElement);
    
    // Orbit Controls
    const controls = new OrbitControls(camera, renderer.domElement);
    controls.minDistance = 2;
    controls.maxDistance = 10000;
    controls.enableDamping = true;
    controls.dampingFactor = 0.1;
    controls.rotateSpeed = 0.5;
    
    // Lights
    scene.add(new THREE.AmbientLight(0xffffff, 1));
    const dirLight = new THREE.DirectionalLight(0xffffff, 1.5);
    dirLight.position.set(10, 20, 10);
    scene.add(dirLight);
    
    // Create a Raycaster (reuse)
    const raycaster = new THREE.Raycaster();
    
    // Load models
    const loader = new GLTFLoader();
    loader.load(demModelPath, (demGltf) => {
      const demModel = demGltf.scene;
      scene.add(demModel);
      
      loader.load(osmModelPath, (osmGltf) => {
        const osmModel = osmGltf.scene;
        
        // Initial alignment: use bounding boxes to roughly position OSM on DEM
        const demBox = new THREE.Box3().setFromObject(demModel);
        const osmBox = new THREE.Box3().setFromObject(osmModel);
        const offsetY = demBox.max.y - osmBox.min.y;
        osmModel.position.y += offsetY;
        const demCenter = new THREE.Vector3();
        demBox.getCenter(demCenter);
        const osmCenter = new THREE.Vector3();
        osmBox.getCenter(osmCenter);
        osmModel.position.x += (demCenter.x - osmCenter.x);
        osmModel.position.z += (demCenter.z - osmCenter.z);
        
        // Now apply shrinkwrap to the entire OSM layer.
        // This will modify vertices near the base to "hug" the terrain.
        shrinkwrapBuildingLayer(osmModel, demModel, raycaster, 0.1);
        
        scene.add(osmModel);
      });
    });
    
    // Render loop
    const animate = () => {
      requestAnimationFrame(animate);
      controls.update();
      renderer.render(scene, camera);
    };
    animate();
    
    return () => {
      container.removeChild(renderer.domElement);
      renderer.dispose();
    };
  }, [cameraPosition]);

  // (Optional) Merge and Export functionality remains here...
  const handleMergeAndExport = () => {
    const loader = new GLTFLoader();
    Promise.all([
      new Promise((resolve, reject) => {
        loader.load(demModelPath, (gltf) => resolve(gltf.scene), undefined, reject);
      }),
      new Promise((resolve, reject) => {
        loader.load(osmModelPath, (gltf) => resolve(gltf.scene), undefined, reject);
      })
    ])
      .then(([demModel, osmModel]) => {
        // Rough alignment as before:
        const demBox = new THREE.Box3().setFromObject(demModel);
        const osmBox = new THREE.Box3().setFromObject(osmModel);
        const offsetY = demBox.max.y - osmBox.min.y;
        osmModel.position.y += offsetY;
        const demCenter = new THREE.Vector3();
        demBox.getCenter(demCenter);
        const osmCenter = new THREE.Vector3();
        osmBox.getCenter(osmCenter);
        osmModel.position.x += (demCenter.x - osmCenter.x);
        osmModel.position.z += (demCenter.z - osmCenter.z);
        
        // Apply shrinkwrap (using the same function)
        const raycaster = new THREE.Raycaster();
        shrinkwrapBuildingLayer(osmModel, demModel, raycaster, 0.1);
        
        // Merge geometries from DEM and OSM
        const geometries = [];
        demModel.traverse(child => {
          if (child.isMesh && child.geometry) {
            child.updateMatrix();
            geometries.push(child.geometry.clone().applyMatrix4(child.matrix));
          }
        });
        osmModel.traverse(child => {
          if (child.isMesh && child.geometry) {
            child.updateMatrix();
            geometries.push(child.geometry.clone().applyMatrix4(child.matrix));
          }
        });
        if (geometries.length === 0) {
          console.error("No geometries found to merge.");
          return;
        }
        const mergedGeometry = mergeGroups(geometries, true);
        const mergedMesh = new THREE.Mesh(mergedGeometry, new THREE.MeshStandardMaterial({ color: 0xcccccc }));
        
        // Rotate for printing: e.g. -90° about X
        mergedMesh.applyMatrix4(new THREE.Matrix4().makeRotationX(THREE.MathUtils.degToRad(-90)));
        
        // Export as STL
        const exporter = new STLExporter();
        const stlString = exporter.parse(mergedMesh);
        const blob = new Blob([stlString], { type: "text/plain" });
        const a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = "merged_model.stl";
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
      })
      .catch(err => console.error("Error merging models:", err));
  };

  return (
    <div style={{ width: "100vw", height: "100vh", background: "#f0f0f0" }}>
      <h2 style={{ color: "#000", textAlign: "center" }}>3D Model Debug Page</h2>
      <div ref={containerRef} style={{ width: "100%", height: "90%" }} />
      <button onClick={handleMergeAndExport} style={{ display: "block", margin: "0 auto" }}>
        Merge and Export STL
      </button>
    </div>
  );
}

export default LayerPage;
