// frontend/components/layout/Sidebar.tsx
"use client";

import Link from "next/link";
import { Package } from "lucide-react";
import { cn } from "@/lib/utils";

type SidebarProps = {
  className?: string;
};

export function Sidebar({ className }: SidebarProps) {
  return (
    <aside
      className={cn(
        "hidden md:block bg-[#0f2740] text-white",
        className
      )}
    >
      <div className="sticky top-0 h-screen overflow-y-auto p-6">
        {/* Header / Branding */}
        <div className="mb-8">
          <h1 className="text-2xl font-bold">Sistema</h1>
          <p className="text-sm text-gray-400 mt-1">Gerenciamento</p>
        </div>

        {/* Nav */}
        <nav className="space-y-2">
          <Link
            href="/orders"
            className="flex items-center gap-3 px-4 py-3 rounded-lg bg-white/10 text-white font-medium hover:bg-white/20 transition-colors"
          >
            <Package className="h-5 w-5" />
            <span>Pedidos</span>
          </Link>
        </nav>
      </div>
    </aside>
  );
}
