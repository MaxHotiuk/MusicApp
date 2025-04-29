# Music Library Analyzer & Recommender

![App Preview](https://via.placeholder.com/800x400?text=Music+Library+Analyzer+Screenshot)  
*A PWA for deep music exploration (concept screenshot)*

## 🎯 About

A privacy-focused web app that helps you:
- **Fix messy metadata** in your local music collection (e.g., unify "The Cure" vs "CURE").
- **Get smart recommendations** via Last.fm (e.g., "Fans of gothic rock also enjoy post-punk").
- **Visualize music history** through genre timelines and artist influence graphs.

⚠️ **No file sharing** – your music never leaves your device.

## ✨ Features

### Local File Management
- Drag-and-drop metadata editor
- Auto-suggestions from Discogs/MusicBrainz
- Batch rename inconsistent tags

### Music Discovery
- Last.fm integration (OAuth login)
- Recommendations by:
  - Subgenre connections
  - Artist influences
  - "Waves" (e.g., 2nd wave black metal)

### Data Visualization
- Interactive artist relationship graphs
- Genre evolution timelines

## 🛠️ Tech Stack

| Component       | Technology               |
|-----------------|--------------------------|
| Frontend        | React + TypeScript       |
| Charts          | D3.js                    |
| Data            | IndexedDB (local storage)|
| APIs            | Last.fm, Discogs         |
| Deployment      | Vercel/Netlify (static)  |

## 🚀 Getting Started

1. **Prerequisites**:
   - Node.js v18+
   - Last.fm API key ([get one here](https://www.last.fm/api))

2. **Development**:
   ```bash
   git clone https://github.com/your-repo/music-analyzer.git
   cd music-analyzer
   npm install
   npm run dev
