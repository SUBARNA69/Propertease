<script>
    $(document).ready(function() {
        // Image Gallery Functionality
        var currentIndex = 0;
    var totalImages = @Model.ImageUrl.Count;
        var imageUrls = [@Html.Raw(string.Join(",", Model.ImageUrl.Select(url => $"'{url}'")))];

    // Handle thumbnail clicks
    $('.thumbnail').click(function() {
            var index = $(this).data('index');
    var src = $(this).data('src');

    // Update main image
    $('#mainImage').attr('src', src);

    // Update active thumbnail
    $('.thumbnail').removeClass('border-blue-500').addClass('border-transparent');
    $(this).removeClass('border-transparent').addClass('border-blue-500');

    // Update current index
    currentIndex = index;
        });

    // Open fullscreen on main image click
    $('#mainImage').click(function() {
        openFullscreen(currentIndex);
        });

    // Close fullscreen
    $('#closeFullscreenImage').click(function() {
        $('#imageFullscreenModal').addClass('hidden');
    $('body').css('overflow', '');
        });

    // Close fullscreen on background click
    $('#imageFullscreenModal').click(function(e) {
            if (e.target === this) {
        $(this).addClass('hidden');
    $('body').css('overflow', '');
            }
        });

    // Previous image button
    $('#prevImageBtn').click(function(e) {
        e.stopPropagation();
    navigateFullscreen(-1);
        });

    // Next image button
    $('#nextImageBtn').click(function(e) {
        e.stopPropagation();
    navigateFullscreen(1);
        });

    // Keyboard navigation
    $(document).keydown(function(e) {
            if ($('#imageFullscreenModal').hasClass('hidden')) return;

    if (e.key === 'Escape') {
        $('#imageFullscreenModal').addClass('hidden');
    $('body').css('overflow', '');
            } else if (e.key === 'ArrowRight') {
        navigateFullscreen(1);
            } else if (e.key === 'ArrowLeft') {
        navigateFullscreen(-1);
            }
        });

    // Helper function to open fullscreen
    function openFullscreen(index) {
        $('#fullscreenImage').attr('src', imageUrls[index]);
    $('#currentFullscreenIndex').text(index + 1);
    $('#imageFullscreenModal').removeClass('hidden');
    $('body').css('overflow', 'hidden'); // Prevent scrolling
    currentIndex = index;
        }

    // Helper function to navigate in fullscreen
    function navigateFullscreen(direction) {
            var newIndex = (currentIndex + direction + totalImages) % totalImages;
    openFullscreen(newIndex);

    // Also update the main image and thumbnails
    $('#mainImage').attr('src', imageUrls[newIndex]);
    $('.thumbnail').removeClass('border-blue-500').addClass('border-transparent');
    $('.thumbnail[data-index="' + newIndex + '"]').removeClass('border-transparent').addClass('border-blue-500');
        }

    // Mapbox initialization
    mapboxgl.accessToken = 'pk.eyJ1Ijoia3Jvc3NzdWJhcm5hIiwiYSI6ImNtN3NteGp5ajE2dWgyanNibHNxc2o5YXcifQ.GSxJY20oRcqu6qnF154OEg';

    var map = new mapboxgl.Map({
        container: 'map',
    style: 'mapbox://styles/mapbox/streets-v11',
    center: [@Model.Longitude, @Model.Latitude],
    zoom: 12
        });

    // Add navigation controls
    map.addControl(new mapboxgl.NavigationControl());

    // Add property marker
    new mapboxgl.Marker()
    .setLngLat([@Model.Longitude, @Model.Latitude])
    .addTo(map);

    // Map enhancement features
    const mapContainer = document.getElementById('mapContainer');
    const mapElement = document.getElementById('map');
    const fullscreenBtn = document.getElementById('fullscreenBtn');
    const showDistanceBtn = document.getElementById('showDistanceBtn');
    const distanceInfo = document.getElementById('distanceInfo');
    const distanceValue = document.getElementById('distanceValue');
    const durationValue = document.getElementById('durationValue');

    let isFullscreen = false;
    let routeShown = false;
    let userMarker = null;

    // Toggle fullscreen
    fullscreenBtn.addEventListener('click', function() {
        isFullscreen = !isFullscreen;

    if (isFullscreen) {
        // Make map fullscreen
        mapContainer.style.position = 'fixed';
    mapContainer.style.top = '0';
    mapContainer.style.left = '0';
    mapContainer.style.width = '100%';
    mapContainer.style.height = '100%';
    mapContainer.style.zIndex = '9999';
    mapContainer.style.backgroundColor = 'white';
    mapElement.style.height = '100%';
    mapElement.style.borderRadius = '0';

    // Change icon to minimize
    fullscreenBtn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M8 3v3a2 2 0 0 1-2 2H3"></path><path d="M21 8h-3a2 2 0 0 1-2-2V3"></path><path d="M3 16h3a2 2 0 0 1 2 2v3"></path><path d="M16 21v-3a2 2 0 0 1 2-2h3"></path></svg>';
            } else {
        // Restore original size
        mapContainer.style.position = 'relative';
    mapContainer.style.top = 'auto';
    mapContainer.style.left = 'auto';
    mapContainer.style.width = '100%';
    mapContainer.style.height = 'auto';
    mapContainer.style.zIndex = '1';
    mapContainer.style.backgroundColor = 'transparent';
    mapElement.style.height = '48px';
    mapElement.style.borderRadius = '0.5rem';

    // Change icon back to expand
    fullscreenBtn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M8 3H5a2 2 0 0 0-2 2v3"></path><path d="M21 8V5a2 2 0 0 0-2-2h-3"></path><path d="M3 16v3a2 2 0 0 0 2 2h3"></path><path d="M16 21h3a2 2 0 0 0 2-2v-3"></path></svg>';
            }

    // Resize map after DOM changes
    setTimeout(function() {
        map.resize();
            }, 100);
        });

    // Show distance functionality
    showDistanceBtn.addEventListener('click', function() {
            if (routeShown) {
                // Remove existing route and markers
                if (userMarker) userMarker.remove();
    if (map.getLayer('route')) map.removeLayer('route');
    if (map.getSource('route')) map.removeSource('route');
    distanceInfo.classList.add('hidden');

    routeShown = false;
    showDistanceBtn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="3 11 22 2 13 21 11 13 3 11"></polygon></svg> <span class="text-xs">Show Distance</span>';
    return;
            }

    // Request user location
    if (navigator.geolocation) {
        showDistanceBtn.disabled = true;
    showDistanceBtn.innerHTML = '<span class="text-xs">Loading...</span>';

    navigator.geolocation.getCurrentPosition(
    function(position) {
                        const userCoords = [position.coords.longitude, position.coords.latitude];

    // Add user location marker
    userMarker = new mapboxgl.Marker({color: '#3887be' })
    .setLngLat(userCoords)
    .setPopup(new mapboxgl.Popup({offset: 25 }).setText('Your Location'))
    .addTo(map);

    // Get directions from Mapbox Directions API
    fetch(`https://api.mapbox.com/directions/v5/mapbox/driving/${userCoords[0]},${userCoords[1]};${@Model.Longitude},${@Model.Latitude}?steps=true&geometries=geojson&access_token=${mapboxgl.accessToken}`)
                            .then(response => response.json())
                            .then(data => {
                                if (!data.routes || data.routes.length === 0) {
                                    throw new Error('No route found');
                                }

    const route = data.routes[0];

    // Calculate distance and duration
    const distanceInKm = (route.distance / 1000).toFixed(1);
    const durationInMinutes = Math.round(route.duration / 60);

    // Display distance information
    distanceValue.textContent = `${distanceInKm} km`;
    durationValue.textContent = `${durationInMinutes} min`;
    distanceInfo.classList.remove('hidden');

    // Add route to map
    map.addSource('route', {
        type: 'geojson',
    data: {
        type: 'Feature',
    properties: { },
    geometry: route.geometry
                                    }
                                });

    map.addLayer({
        id: 'route',
    type: 'line',
    source: 'route',
    layout: {
        'line-join': 'round',
    'line-cap': 'round'
                                    },
    paint: {
        'line-color': '#3887be',
    'line-width': 5,
    'line-opacity': 0.75
                                    }
                                });

    // Fit map to show both points
    const bounds = new mapboxgl.LngLatBounds();
    bounds.extend(userCoords);
    bounds.extend([@Model.Longitude, @Model.Latitude]);

    map.fitBounds(bounds, {
        padding: 50,
    maxZoom: 15
                                });

    routeShown = true;
    showDistanceBtn.disabled = false;
    showDistanceBtn.innerHTML = '<span class="text-xs">Hide Route</span>';
                            })
                            .catch(error => {
        console.error('Error getting directions:', error);
    alert('Unable to calculate directions. Please try again later.');
    showDistanceBtn.disabled = false;
    showDistanceBtn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="3 11 22 2 13 21 11 13 3 11"></polygon></svg> <span class="text-xs">Show Distance</span>';
                            });
                    },
    function(error) {
        console.error('Error getting location:', error);
    let errorMessage = 'Unable to get your location.';

    switch(error.code) {
                            case error.PERMISSION_DENIED:
    errorMessage += ' Please allow location access.';
    break;
    case error.POSITION_UNAVAILABLE:
    errorMessage += ' Location information is unavailable.';
    break;
    case error.TIMEOUT:
    errorMessage += ' The request timed out.';
    break;
                        }

    alert(errorMessage);
    showDistanceBtn.disabled = false;
    showDistanceBtn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="3 11 22 2 13 21 11 13 3 11"></polygon></svg> <span class="text-xs">Show Distance</span>';
                    },
    {
        enableHighAccuracy: true,
    timeout: 5000,
    maximumAge: 0
                    }
    );
            } else {
        alert('Geolocation is not supported by this browser.');
            }
        });

    // Schedule Viewing Modal
    const modal = document.getElementById('scheduleViewingModal');
    const openModalBtn = document.getElementById('scheduleViewingBtn');
    const closeModalBtn = document.getElementById('closeModal');

    // Open modal and fetch user data
    openModalBtn.addEventListener('click', function() {
        modal.classList.remove('hidden');

    // Fetch buyer details if user is logged in
    fetch('/Home/GetBuyerDetails')
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
        document.getElementById('buyerName').value = data.name || '';
    document.getElementById('buyerEmail').value = data.email || '';
    document.getElementById('buyerContact').value = data.contactNumber || '';
                    }
                })
                .catch(error => console.error('Error fetching buyer details:', error));
        });

    // Close modal when clicking the close button
    closeModalBtn.addEventListener('click', function() {
        modal.classList.add('hidden');
        });

    // Close modal when clicking outside of the modal content
    window.addEventListener('click', function(event) {
            if (event.target === modal) {
        modal.classList.add('hidden');
            }
        });

    // Seller Profile Functionality
    const sellerImage = document.getElementById('sellerImage');
    const sellerOptions = document.getElementById('sellerOptions');
    const sellerProfileModal = document.getElementById('sellerProfileModal');
    const reportSellerModal = document.getElementById('reportSellerModal');
    const viewProfileBtn = document.getElementById('viewProfileBtn');
    const reportSellerBtn = document.getElementById('reportSellerBtn');
    const closeProfileModal = document.getElementById('closeProfileModal');
    const closeReportModal = document.getElementById('closeReportModal');
    const closeReportBtn = document.getElementById('closeReportBtn');

    // Toggle dropdown menu when clicking on seller image
    sellerImage.addEventListener('click', function(event) {
        event.stopPropagation();
    sellerOptions.classList.toggle('hidden');
        });

    // Close dropdown when clicking elsewhere
    document.addEventListener('click', function() {
        sellerOptions.classList.add('hidden');
        });

    // Prevent dropdown from closing when clicking inside it
    sellerOptions.addEventListener('click', function(event) {
        event.stopPropagation();
        });

    // Open profile modal when clicking "Profile" option
    viewProfileBtn.addEventListener('click', function(event) {
        event.preventDefault();
    sellerOptions.classList.add('hidden');
    sellerProfileModal.classList.remove('hidden');
        });

    // Open report modal when clicking "Report" option
    reportSellerBtn.addEventListener('click', function(event) {
        event.preventDefault();
    sellerOptions.classList.add('hidden');
    reportSellerModal.classList.remove('hidden');
        });

    // Close profile modal
    closeProfileModal.addEventListener('click', function() {
        sellerProfileModal.classList.add('hidden');
        });

    // Close report modal
    closeReportModal.addEventListener('click', function() {
        reportSellerModal.classList.add('hidden');
        });

    closeReportBtn.addEventListener('click', function() {
        reportSellerModal.classList.add('hidden');
        });

    // Close modals when clicking outside
    window.addEventListener('click', function(event) {
            if (event.target === sellerProfileModal) {
        sellerProfileModal.classList.add('hidden');
            }
    if (event.target === reportSellerModal) {
        reportSellerModal.classList.add('hidden');
            }
    if (event.target === chatModal) {
        chatModal.classList.add('hidden');
            }
        });

    $(document).ready(function() {
        // Check if property is already in compare list
        function checkCompareStatus() {
            $.ajax({
                url: '@Url.Action("GetCompareList", "Home")',
                type: 'GET',
                success: function (response) {
                    if (response.properties && response.properties.includes('@Model.Id')) {
                        $('#addToCompareBtn').addClass('bg-green-600').removeClass('bg-blue-600');
                        $('#addToCompareBtn').html('<svg class="w-4 h-4 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg> Added to Compare');
                    }

                    // Show compare count if any properties are in the list
                    if (response.count > 0) {
                        $('#compareCount').text(response.count).parent().removeClass('hidden');
                    }
                }
            });
        }

            // Add to compare button click handler
            $('#addToCompareBtn').click(function() {
        $.ajax({
            url: '@Url.Action("AddToCompare", "Home")',
            type: 'POST',
            data: { id: '@Model.Id' },
            success: function (response) {
                if (response.success) {
                    $('#addToCompareBtn').addClass('bg-green-600').removeClass('bg-blue-600');
                    $('#addToCompareBtn').html('<svg class="w-4 h-4 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg> Added to Compare');

                    // Update compare count
                    $('#compareCount').text(response.count);
                    $('#compareCountContainer').removeClass('hidden');
                } else {
                    alert(response.message || 'Error adding property to compare');
                }
            }
        });
            });

    // Check status on page load
    checkCompareStatus();
        });
    });
</script>