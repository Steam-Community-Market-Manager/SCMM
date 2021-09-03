
import * as THREE from 'https://cdn.skypack.dev/three@v0.131.3';
import { WEBGL } from 'https://cdn.skypack.dev/three@v0.131.3/examples/jsm/WebGL.js';
import { Loader } from 'https://cdn.skypack.dev/three@v0.131.3/src/loaders/Loader.js';
import { OBJLoader } from 'https://cdn.skypack.dev/three@v0.131.3/examples/jsm/loaders/OBJLoader.js';
import { TGALoader } from 'https://cdn.skypack.dev/three@v0.131.3/examples/jsm/loaders/TGALoader.js';
import { OrbitControls } from 'https://cdn.skypack.dev/three@v0.131.3/examples/jsm/controls/OrbitControls.js';
import ZipLoader from 'https://cdn.jsdelivr.net/npm/zip-loader@1.1.0/dist/ZipLoader.module.js';

// TODO: Make this instanced rather than global
let loadingManager, workshopFileLoader, objLoader, tgaLoader, textureLoader;
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
		throw 'Your browser (or graphics card) does not support WebGL';
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

	workshopFileLoader = new ZipLoader(options.workshopFileUrl);
	workshopFileLoader.on('progress', function (event) {
		callback.invokeMethodAsync('OnLoadProgress', event.loaded, event.total);
	});
	workshopFileLoader.on('load', function (event) {
		callback.invokeMethodAsync('OnLoadComplete');
		try {
			addWorkshopFileToScene(workshopFileLoader, scene);
		}
		catch (error) {
			callback.invokeMethodAsync('OnLoadError', error);
        }
	});
	workshopFileLoader.on('error', function (event) {
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
	controls.autoRotate = (options.autoRotate !== undefined ? options.autoRotate : false);
	controls.enableDamping = true;
	/*
	controls.listenToKeyEvents(window);
	controls.keys = {
		LEFT: 'KeyA',
		UP: 'KeyW',
		RIGHT: 'KeyD',
		BOTTOM: 'KeyS'
	}
	*/

	// Setup scene	
	scene = new THREE.Scene();
	if (options.background) {
		scene.background = new THREE.Color(options.background);
	}

	// Setup lighting
	light = new THREE.PointLight(new THREE.Color(1, 1, 1), 1, 0);
	light.position.set(0, 0, 0);
	light.visible = (options.light !== undefined ? options.light : true);
	light.castShadow = true;
	scene.add(light);

	// Show the renderer
	container.appendChild(renderer.domElement);
	resized = true;

	// Start loading the workshop file
	workshopFileLoader.load();
	return scene;
}

export function resetCamera(instance) {
	var bb = new THREE.Box3()
	bb.setFromObject(model);
	bb.getCenter(controls.target);
	camera.position.set(0, 0, 3);
}

export function toggleAutoRotate(instance, toggled) {
	controls.autoRotate = toggled;
}

export function toggleAlphaCutoff(instance, toggled) {
	model.traverse(function (child) {
		if (child instanceof THREE.Mesh) {
			child.material.needsUpdate = true;
			child.material.userData = (child.material.userData || {});
			if (toggled) {
				child.material.userData.lastAlphaTest = child.material.alphaTest;
				child.material.alphaTest = 0;
			} else {
				child.material.alphaTest = (child.material.userData.lastAlphaTest || 0);
            }
		}
	});
}

export function toggleLight(instance, toggled) {
	light.visible = toggled;
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

function addWorkshopFileToScene(loader, scene) {

	if (!loader.files.hasOwnProperty('manifest.txt')) {
		throw 'No skin manifest detected, this is probably a very old skin';
	}
	var manifest = loader.extractAsJSON('manifest.txt');
	if (manifest == null) {
		throw 'Unable to load manifest.txt';
	}

	var endpointUrl = options.modelEndpoint;
	var azureBlobLoader = new AzureBlobLoader(loadingManager);
	azureBlobLoader.load(
		{
			endpoint: endpointUrl.substr(0, endpointUrl.lastIndexOf('/')),
			container: endpointUrl.substr(endpointUrl.lastIndexOf('/') + 1),
			prefix: options.modelName + '/'
		},
		function (blobs) {
			blobs.forEach(function (blob) {
				var manifestIndex = parseInt(blob.metadata.index || '0');
				if (manifestIndex > manifest.Groups.length - 1) {
					manifestIndex = 0;
                }
				if (blob.metadata.hidden) {
					return;
                }
				objLoader.load(
					(endpointUrl + '/' + blob.name),
					function (object) {

						object.castShadow = true;
						object.receiveShadow = true;
						object.traverse(function (child) {
							if (child instanceof THREE.Mesh) {

								var manifestMesh = manifest.Groups[manifestIndex];
								if (manifestMesh == null) {
									throw 'Unable to find manifest group (' + manifestIndex + ') for mesh #' + child.id;
								}

								child.castShadow = true;
								child.receiveShadow = true;
								child.material.needsUpdate = true;

								var map = loadWorkshopFileTexture(loader, manifestMesh.Textures._MainTex);
								var normalMap = loadWorkshopFileTexture(loader, manifestMesh.Textures._BumpMap);
								var lightMap = loadWorkshopFileTexture(loader, manifestMesh.Textures._OcclusionMap);
								var specularMap = loadWorkshopFileTexture(loader, manifestMesh.Textures._SpecGlossMap);
								var emissiveMap = loadWorkshopFileTexture(loader, manifestMesh.Textures._EmissionMap);

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

								child.material.side = THREE.DoubleSide;
								child.material.shadowSide = THREE.DoubleSide;

							}
						});

						if (blob.metadata.positionx) {
							object.position.x = parseFloat(blob.metadata.positionx);
						}
						if (blob.metadata.positiony) {
							object.position.y = parseFloat(blob.metadata.positiony);
						}
						if (blob.metadata.positionz) {
							object.position.z = parseFloat(blob.metadata.positionz);
						}
						if (blob.metadata.scalex) {
							object.scale.x = parseFloat(blob.metadata.scalex);
						} else {
							object.scale.x = -1; // by default invert objects horizontally, seems that most skins need this
                        }
						if (blob.metadata.scaley) {
							object.scale.y = parseFloat(blob.metadata.scaley);
						}

						model = object;
						scene.add(model);

						if (manifestIndex == 0) {
							resetCamera();
						}

					}
				);
			});
		}
	);
}

function loadWorkshopFileTexture(loader, name) {
	if (name == null || !loader.files.hasOwnProperty(name)) {
		return null;
	}
	const mimeType = (
		(/\.png$/).test(name) ? 'image/png' :
		(/\.jp[e]*[g]*$/).test(name) ? 'image/jpeg' :
		(/\.bmp$/).test(name) ? 'image/bmp' :
		(/\.tga$/).test(name) ? 'image/tga' :
		undefined
	);
	const textureUrl = loader.extractAsBlobUrl(name, mimeType)
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

class AzureBlobLoader extends Loader {

	load(url, onLoad, onProgress, onError) {
		try
		{
			const blobService = AzureStorage.Blob.createBlobServiceAnonymous(url.endpoint);
			const blobs = blobService.listBlobsSegmentedWithPrefix(url.container, url.prefix, null, { include: 'metadata' },
				function (error, result) {
					if (result && result.entries) {
						onLoad(result.entries);
					}
					else {
						onError(error);
					}
				}
			);
		} catch (ex) {
			onError(ex);
        }
	}

}
