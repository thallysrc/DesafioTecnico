/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,ts,tsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          50:  '#EFF4FC',
          100: '#D6E2F7',
          200: '#A6C0EE',
          300: '#759DE5',
          400: '#447BDC',
          500: '#1863DC',
          600: '#0056A7',
          700: '#003F7D',
          800: '#002A54',
          900: '#001530',
        },
        ink:     '#212121',
        muted:   '#7B7B7B',
        line:    '#EBEBEB',
        surface: '#F4F4F4',
        success: '#009C34',
        warning: '#FCB900',
        danger:  '#CF2E2E',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
}
