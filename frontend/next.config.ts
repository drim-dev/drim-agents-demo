import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  devIndicators: false,
  experimental: {
    staleTimes: {
      dynamic: 0,
      static: 30,
    },
  },
  async rewrites() {
    return [
      {
        source: "/hubs/chat",
        destination: `${process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000"}/hubs/chat`,
      },
    ];
  },
  async headers() {
    return [
      {
        source: "/_next/static/:path*",
        headers: [
          {
            key: "Cache-Control",
            value: "public, max-age=31536000, immutable",
          },
        ],
      },
      {
        source: "/",
        headers: [
          {
            key: "Cache-Control",
            value: "public, max-age=3600, s-maxage=604800",
          },
        ],
      },
    ];
  },
};

export default nextConfig;
