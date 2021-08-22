
import * as THREE from 'https://cdn.skypack.dev/three@v0.131.3';
import { WEBGL } from 'https://cdn.skypack.dev/three@v0.131.3/examples/jsm/WebGL.js';
import { OBJLoader } from 'https://cdn.skypack.dev/three@v0.131.3/examples/jsm/loaders/OBJLoader.js';
import { TGALoader } from 'https://cdn.skypack.dev/three@v0.131.3/examples/jsm/loaders/TGALoader.js';
import { OrbitControls } from 'https://cdn.skypack.dev/three@v0.131.3/examples/jsm/controls/OrbitControls.js';
import ZipLoader from 'https://cdn.jsdelivr.net/npm/zip-loader@1.1.0/dist/ZipLoader.module.js';

// TODO: Make this instanced rather than global
let loadingManager, zipLoader, objLoader, tgaLoader, textureLoader;
let renderer, container, options, callback;
let camera, controls;
let scene, model, light;
let resized = false;

export function createSceneFromWorkshopFile(sceneContainer, sceneOptions, dotNetHelper) {

	container = sceneContainer;
	options = sceneOptions;
	callback = dotNetHelper;

	// Sanity check
	if (!WEBGL.isWebGLAvailable()) {
		console.log('WebGL is not supported by the browser');
		container.appendChild(WEBGL.getWebGLErrorMessage());
		return;
	}

	// Setup file loaders
	loadingManager = new THREE.LoadingManager();
	loadingManager.onStart = function (url, itemsLoaded, itemsTotal) {
		callback.invokeMethodAsync('OnLoadProgress', itemsLoaded, itemsTotal);
	};
	loadingManager.onProgress = function (url, itemsLoaded, itemsTotal) {
		callback.invokeMethodAsync('OnLoadProgress', itemsLoaded, itemsTotal);
	};
	loadingManager.onLoad = function () {
		callback.invokeMethodAsync('OnLoadComplete');
	};
	loadingManager.onError = function (url) {
		callback.invokeMethodAsync('OnLoadError', 'There was an error downloading ' + url);
	};

	zipLoader = new ZipLoader(options.workshopFileUrl);
	zipLoader.on('progress', function (event) {
		callback.invokeMethodAsync('OnLoadProgress', event.loaded, event.total);
	});
	zipLoader.on('load', function (event) {
		callback.invokeMethodAsync('OnLoadComplete');
		try {
			addWorkshopFileToScene(zipLoader.files, scene);
		}
		catch (error) {
			callback.invokeMethodAsync('OnLoadError', event.error);
        }
	});
	zipLoader.on('error', function (event) {
		callback.invokeMethodAsync('OnLoadError', event.error);
	});

	objLoader = new OBJLoader(loadingManager);
	tgaLoader = new TGALoader(loadingManager);
	textureLoader = new THREE.TextureLoader(loadingManager);

	// Setup renderer
	renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
	renderer.setAnimationLoop(renderLoop);
	
	// Setup camera and controls
	camera = new THREE.PerspectiveCamera(50, 1, 0.01, 100);
	camera.position.z = 1;
	controls = new OrbitControls(camera, renderer.domElement);
	controls.enableDamping = true;
	controls.autoRotate = (options.autoRotate !== undefined ? options.autoRotate : false);

	// Setup scene	
	scene = new THREE.Scene();
	if (options.background) {
		scene.background = new THREE.Color(options.background);
	}

	// Setup lighting
	light = new THREE.PointLight(new THREE.Color(1, 1, 1), 1, 0);
	light.position.set(0, 0, 0);
	light.castShadow = true;
	light.visible = false;
	scene.add(light);

	// Show the renderer
	container.appendChild(renderer.domElement);
	resized = true;

	// Start loading the workshop file
	zipLoader.load();
	return scene;
}

export function resetCamera(instance) {
	var bb = new THREE.Box3()
	bb.setFromObject(model);
	bb.getCenter(controls.target);
	camera.position.set(0, 0, 3);
}

export function toggleAutoRotate(instance) {
	controls.autoRotate = !controls.autoRotate;
}

export function toggleAlphaCutoff(instance) {
	model.traverse(function (child) {
		if (child instanceof THREE.Mesh) {
			child.material.needsUpdate = true;
			if (child.material.alphaTest > 0) {
				child.material.alphaTestOriginal = child.material.alphaTest;
				child.material.alphaTest = 0;
			} else {
				child.material.alphaTest = child.material.alphaTestOriginal;
            }
		}
	});
}

export function toggleLight(instance) {
	light.visible = !light.visible;
}

export function setLightColor(instance, color) {
	light.color = new THREE.Color(color);
}

export function setLightIntensity(instance, intensity) {
	light.intensity = intensity;
}

export function setEmissionIntensity(instance, intensity) {
	model.traverse(function (child) {
		if (child instanceof THREE.Mesh) {
			if (child.material.emissiveMap != null) {
				child.material.emissiveIntensity = intensity;
            }
		}
	});
}

function addWorkshopFileToScene(files, scene) {

	var manifest = zipLoader.extractAsJSON('manifest.txt');
	if (manifest == null) {
		throw 'Unable to find manifest.txt';
	}

	objLoader.load(
		options.modelUrl,
		function (object) {

			object.castShadow = true;
			object.receiveShadow = true;
			object.traverse(function (child) {
				if (child instanceof THREE.Mesh) {

					var manifestMesh = manifest.Groups[0];
					if (manifestMesh == null) {
						throw 'Unable to find manifest group for mesh #' + child.id;
					}

					child.castShadow = true;
					child.receiveShadow = true;
					child.material.needsUpdate = true;

					var map = loadWorkshopFileTexture(files, manifestMesh.Textures._MainTex);
					var normalMap = loadWorkshopFileTexture(files, manifestMesh.Textures._BumpMap);
					var lightMap = loadWorkshopFileTexture(files, manifestMesh.Textures._OcclusionMap);
					var specularMap = loadWorkshopFileTexture(files, manifestMesh.Textures._SpecGlossMap);
					var emissiveMap = loadWorkshopFileTexture(files, manifestMesh.Textures._EmissionMap);

					// Textures
					child.material.map = map;
					child.material.normalMap = normalMap;
					child.material.lightMap = lightMap;
					child.material.specularMap = specularMap;
					if (emissiveMap) {
						child.material.emissiveMap = emissiveMap;
                    }

					// Floats
					child.material.alphaTest = manifestMesh.Floats._Cutoff;
					child.material.normalScale = new THREE.Vector2(manifestMesh.Floats._BumpScale, manifestMesh.Floats._BumpScale);
					child.material.lightMapIntensity = manifestMesh.Floats._OcclusionStrength;
					child.material.shininess = manifestMesh.Floats._Glossiness;
					if (emissiveMap) {
						child.material.emissiveIntensity = 1;
					}

					// Colors
					child.material.color = new THREE.Color(manifestMesh.Colors._Color.r, manifestMesh.Colors._Color.g, manifestMesh.Colors._Color.b);
					child.material.specular = new THREE.Color(manifestMesh.Colors._SpecColor.r, manifestMesh.Colors._SpecColor.g, manifestMesh.Colors._SpecColor.b);
					if (emissiveMap) {
						child.material.emissive = new THREE.Color(manifestMesh.Colors._EmissionColor.r, manifestMesh.Colors._EmissionColor.g, manifestMesh.Colors._EmissionColor.b);
                    }

				}
			});

			// HACK: Textures show back-to-front... must negatively scale the object to "fix" it
			object.scale.x = -1;

			model = object;
			scene.add(model);
			resetCamera();
		}
	);

}

function loadWorkshopFileTexture(files, name) {
	if (name == null || !files.hasOwnProperty(name)) {
		return null;
	}
	const mimeType = (
		(/\.png$/).test(name) ? 'image/png' :
		(/\.jp[e]*[g]*$/).test(name) ? 'image/jpeg' :
		(/\.bmp$/).test(name) ? 'image/bmp' :
		(/\.tga$/).test(name) ? 'image/tga' :
		undefined
	);
	const textureUrl = zipLoader.extractAsBlobUrl(name, mimeType)
	switch (mimeType) {
		case 'image/tga': return tgaLoader.load(textureUrl);
		default: return textureLoader.load(textureUrl);
    }
}

function renderLoop() {
	if (resized) {
		const width = container.clientWidth;
		const height = container.clientHeight;
		if (container.width !== width || container.height !== height) {
			renderer.setSize(width, height, false); // must pass false here or three.js fights the browser
			camera.aspect = width / height;
			camera.updateProjectionMatrix();
		}
		resized = false;
	}
	controls.update();
	light.position.copy(camera.position);
	renderer.render(scene, camera);
}

window.addEventListener('resize', () => {
	resized = true;
});
