// 3d-model-viewer.js - Client-side code to fetch and display 3D models

// Import THREE.js and required loaders
import * as THREE from "https://cdn.skypack.dev/three@0.129.0/build/three.module.js";
import { OrbitControls } from "https://cdn.skypack.dev/three@0.129.0/examples/jsm/controls/OrbitControls.js";
import { GLTFLoader } from "https://cdn.skypack.dev/three@0.129.0/examples/jsm/loaders/GLTFLoader.js";

class PropertyModelViewer {
    constructor(containerId, propertyId) {
        // Store the property ID
        this.propertyId = propertyId;

        // Get the container element
        this.container = document.getElementById(containerId);
        if (!this.container) {
            console.error(`Container with ID '${containerId}' not found`);
            return;
        }

        // Set up loading indicator
        this.setupLoadingIndicator();

        // Initialize the scene
        this.initScene();

        // Fetch and load the 3D model
        this.fetchAndLoadModel();
    }

    setupLoadingIndicator() {
        this.loadingElement = document.createElement('div');
        this.loadingElement.style.position = 'absolute';
        this.loadingElement.style.top = '50%';
        this.loadingElement.style.left = '50%';
        this.loadingElement.style.transform = 'translate(-50%, -50%)';
        this.loadingElement.style.color = 'white';
        this.loadingElement.style.fontSize = '24px';
        this.loadingElement.style.fontFamily = 'Arial, sans-serif';
        this.loadingElement.style.backgroundColor = 'rgba(0, 0, 0, 0.5)';
        this.loadingElement.style.padding = '20px';
        this.loadingElement.style.borderRadius = '10px';
        this.loadingElement.style.zIndex = '1000';
        this.loadingElement.textContent = 'Loading 3D model...';
        this.container.appendChild(this.loadingElement);
    }

    initScene() {
        // Create scene
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0xf0f0f0);

        // Create camera
        const width = this.container.clientWidth;
        const height = this.container.clientHeight;
        this.camera = new THREE.PerspectiveCamera(75, width / height, 0.1, 1000);
        this.camera.position.set(0, 2, 5);

        // Create renderer
        this.renderer = new THREE.WebGLRenderer({ antialias: true });
        this.renderer.setSize(width, height);
        this.renderer.setPixelRatio(window.devicePixelRatio);
        this.renderer.shadowMap.enabled = true;
        this.container.appendChild(this.renderer.domElement);

        // Add lights
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.5);
        this.scene.add(ambientLight);

        const directionalLight = new THREE.DirectionalLight(0xffffff, 1);
        directionalLight.position.set(5, 5, 5);
        directionalLight.castShadow = true;
        directionalLight.shadow.mapSize.width = 2048;
        directionalLight.shadow.mapSize.height = 2048;
        this.scene.add(directionalLight);

        // Add ground plane
        const groundGeometry = new THREE.PlaneGeometry(100, 100);
        const groundMaterial = new THREE.MeshStandardMaterial({
            color: 0xcccccc,
            roughness: 0.8,
            metalness: 0.2
        });
        const ground = new THREE.Mesh(groundGeometry, groundMaterial);
        ground.rotation.x = -Math.PI / 2;
        ground.position.y = -1;
        ground.receiveShadow = true;
        this.scene.add(ground);

        // Add orbit controls
        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;

        // Handle window resize
        window.addEventListener('resize', () => this.onWindowResize());

        // Start animation loop
        this.animate();
    }

    onWindowResize() {
        const width = this.container.clientWidth;
        const height = this.container.clientHeight;

        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(width, height);
    }

    animate() {
        requestAnimationFrame(() => this.animate());
        this.controls.update();
        this.renderer.render(this.scene, this.camera);
    }

    async fetchAndLoadModel() {
        try {
            // First, fetch property details to get the 3D model filename
            const response = await fetch(`/Home/ModelViewer/${this.propertyId}`);

            if (!response.ok) {
                throw new Error(`Failed to fetch property details: ${response.statusText}`);
            }

            // Check if the response is HTML (the ModelViewer view)
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('text/html')) {
                // Extract the model filename from the HTML response
                const html = await response.text();
                const modelFilenameMatch = html.match(/data-model-filename="([^"]+)"/);

                if (!modelFilenameMatch) {
                    throw new Error('Could not find 3D model filename in the response');
                }

                const modelFilename = modelFilenameMatch[1];
                this.loadModel(`/3DModels/${modelFilename}`);
            } else {
                // If it's JSON, parse it directly
                const data = await response.json();
                if (data.threeDModel) {
                    this.loadModel(`/3DModels/${data.threeDModel}`);
                } else {
                    throw new Error('No 3D model found for this property');
                }
            }
        } catch (error) {
            console.error('Error fetching property details:', error);
            this.showError(`Failed to load 3D model: ${error.message}`);
        }
    }

    loadModel(modelUrl) {
        const loader = new GLTFLoader();

        loader.load(
            modelUrl,
            (gltf) => {
                // Hide loading indicator
                this.loadingElement.style.display = 'none';

                // Add the model to the scene
                this.model = gltf.scene;

                // Center the model
                const box = new THREE.Box3().setFromObject(this.model);
                const center = box.getCenter(new THREE.Vector3());
                this.model.position.x -= center.x;
                this.model.position.y -= center.y;
                this.model.position.z -= center.z;

                // Scale the model to fit in view
                const size = box.getSize(new THREE.Vector3());
                const maxDim = Math.max(size.x, size.y, size.z);
                if (maxDim > 0) {
                    this.model.scale.multiplyScalar(5.0 / maxDim);
                }

                // Enable shadows for all meshes
                this.model.traverse((child) => {
                    if (child.isMesh) {
                        child.castShadow = true;
                        child.receiveShadow = true;
                    }
                });

                this.scene.add(this.model);

                // Adjust camera to focus on the model
                this.controls.target.copy(center);
                this.controls.update();
            },
            (xhr) => {
                // Update loading progress
                const percentComplete = Math.round((xhr.loaded / xhr.total) * 100);
                this.loadingElement.textContent = `Loading 3D model... ${percentComplete}%`;
            },
            (error) => {
                console.error('Error loading 3D model:', error);
                this.showError(`Failed to load 3D model: ${error.message}`);
            }
        );
    }

    showError(message) {
        this.loadingElement.textContent = message;
        this.loadingElement.style.backgroundColor = 'rgba(255, 0, 0, 0.7)';
    }
}

// Usage example - add this to your ModelViewer.cshtml view
document.addEventListener('DOMContentLoaded', () => {
    // Get the property ID from a data attribute on the container
    const container = document.getElementById('model-container');
    if (container) {
        const propertyId = container.dataset.propertyId;
        if (propertyId) {
            new PropertyModelViewer('model-container', propertyId);
        } else {
            console.error('Property ID not found');
        }
    }
});