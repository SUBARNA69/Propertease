// Initialize the 3D model viewer
function initializeModelViewer(modelPath) {
    // Get the container element
    const container = document.getElementById('model-container');
    const loadingElement = document.getElementById('model-loading');
    const loadingProgress = document.getElementById('loading-progress');

    // Create a scene
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0xf8f9fa); // Light background color

    // Create a camera
    const camera = new THREE.PerspectiveCamera(
        60, // Field of view
        container.clientWidth / container.clientHeight, // Aspect ratio
        0.1, // Near clipping plane
        1000 // Far clipping plane
    );
    camera.position.z = 5; // Initial camera position

    // Create a WebGL renderer
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(container.clientWidth, container.clientHeight);
    renderer.shadowMap.enabled = true; // Enable shadow mapping
    renderer.setPixelRatio(window.devicePixelRatio); // For high DPI displays
    container.appendChild(renderer.domElement);

    // Add lighting
    // Ambient light for overall illumination
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
    scene.add(ambientLight);

    // Directional light for shadows and highlights (like sunlight)
    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
    directionalLight.position.set(1, 1, 1);
    directionalLight.castShadow = true;
    scene.add(directionalLight);

    // Add a soft light from the opposite direction
    const fillLight = new THREE.DirectionalLight(0xffffff, 0.4);
    fillLight.position.set(-1, 0.5, -1);
    scene.add(fillLight);

    // Add orbit controls to allow rotating/zooming the model
    const controls = new THREE.OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true; // Add smooth damping effect
    controls.dampingFactor = 0.25;
    controls.enableZoom = true;
    controls.autoRotate = false; // Initially not rotating
    controls.autoRotateSpeed = 1.0;

    // Store model reference for later use
    let model;
    let modelBoundingBox;
    let modelSize;

    // Create a loading manager to track progress
    const loadingManager = new THREE.LoadingManager();

    loadingManager.onProgress = function (url, itemsLoaded, itemsTotal) {
        const progress = Math.round((itemsLoaded / itemsTotal) * 100);
        loadingProgress.textContent = `Loading 3D Model: ${progress}%`;
    };

    loadingManager.onLoad = function () {
        loadingElement.style.display = 'none';
    };

    loadingManager.onError = function (url) {
        loadingProgress.textContent = 'Error loading model';
        loadingProgress.classList.remove('text-blue-600');
        loadingProgress.classList.add('text-red-600');
    };

    // Load the 3D model
    const loader = new THREE.GLTFLoader(loadingManager);
    loader.load(
        modelPath,
        function (gltf) {
            // Model loaded successfully
            model = gltf.scene;
            scene.add(model);

            // Center the model
            modelBoundingBox = new THREE.Box3().setFromObject(model);
            const center = modelBoundingBox.getCenter(new THREE.Vector3());
            model.position.x = -center.x;
            model.position.y = -center.y;
            model.position.z = -center.z;

            // Adjust camera to fit model
            modelSize = modelBoundingBox.getSize(new THREE.Vector3());
            const maxDim = Math.max(modelSize.x, modelSize.y, modelSize.z);
            camera.position.z = maxDim * 2.5;

            // Update controls target to center of model
            controls.target.set(0, 0, 0);
            controls.update();

            // Apply shadows to the model
            model.traverse(function (node) {
                if (node.isMesh) {
                    node.castShadow = true;
                    node.receiveShadow = true;
                }
            });
