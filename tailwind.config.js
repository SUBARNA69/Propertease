module.exports = {
    content: [
        './Views/**/*.cshtml', // Your Razor views
        './node_modules/flowbite/**/*.js', // Flowbite components
    ],
    theme: {
        extend: {},
    },
    plugins: [
        require('flowbite/plugin'), // Load the Flowbite plugin
    ],
};
